using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using System;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Repositories;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GoogleDrivePushCli.Services;

public class DataAccessService : DataAccessBase
{
    private readonly RootCacheRepository rootCacheRepository;
    private readonly RemoteFileCacheRepository remoteFileCacheRepository;
    private readonly RemoteFolderCacheRepository remoteFolderCacheRepository;

    private static readonly CacheConfiguration cacheConfiguration = ApplicationConfiguration.Instance.Cache;
    private readonly RootCache rootCache;
    private readonly SqliteConnection connection;

    private static DataAccessBase instance;
    public static DataAccessBase Instance
    {
        get
        {
            instance ??= cacheConfiguration.Enabled ?
                new DataAccessService() :
                new DataAccessRepository();
            return instance;
        }
    }

    private DataAccessRepository repository;
    private DataAccessRepository Repository
    {
        get
        {
            repository ??= new();
            return repository;
        }
    }

    private DataAccessService()
    {
        // Create tables if database file doesn't exist
        var shouldCreateTables = !File.Exists(Defaults.cacheDatabasePath);

        // Open connection (will create database if it doesn't exist)
        connection = new SqliteConnection(Defaults.cacheDatabaseConnectionString);
        connection.Open();

        // Initialize repositories
        rootCacheRepository = new(connection);
        remoteFileCacheRepository = new(connection);
        remoteFolderCacheRepository = new(connection);

        // Create tables if needed
        if (shouldCreateTables)
        {
            rootCacheRepository.CreateTable();
            remoteFileCacheRepository.CreateTable();
            remoteFolderCacheRepository.CreateTable();
        }

        // Initialize root cache if needed
        if (!rootCacheRepository.IsInitialized)
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rootId = Repository.GetRootFolder().Id;
            rootCacheRepository.Model = new()
            {
                Timestamp = timestamp,
                RootId = rootId
            };
        }

        // Clear all cache if needed
        rootCache = rootCacheRepository.Model;
        if (CacheIsExpired)
        {
            ConsoleHelpers.Info($"TTL {cacheConfiguration.Ttl} met. Clearing cache.");
            ClearCache();
        }
    }

    public override string RootId => rootCache.RootId;

    public override void CloseConnection()
    {
        connection.Close();
        connection.Dispose();
    }

    public override void ClearCache()
    {
        remoteFileCacheRepository.DeleteAll();
        remoteFolderCacheRepository.DeleteAll();
        GetNextTimestamp();
    }

    private long GetNextTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        rootCache.Timestamp = timestamp;
        rootCacheRepository.Update(rootCache);
        return timestamp;
    }

    private bool CacheIsExpired => rootCache.Timestamp.IsExpired(cacheConfiguration.Ttl);

    public override RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath, IProgress<double> progressReport)
    {
        var remoteFile = Repository.UpdateRemoteFile(remoteFileId, localFilePath, progressReport);
        remoteFile.Timestamp = GetNextTimestamp();
        remoteFileCacheRepository.Upsert(remoteFile);
        return remoteFile;
    }

    public override RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath, IProgress<double> progressReport)
    {
        var remoteFile = Repository.CreateRemoteFile(remoteFolderId, localFilePath, progressReport);
        remoteFile.Timestamp = GetNextTimestamp();
        remoteFileCacheRepository.Insert(remoteFile);
        return remoteFile;
    }

    public override RemoteFolder CreateEmptyRemoteFolder(string parentRemoteFolderId, string folderName)
    {
        var remoteFolder = Repository.CreateEmptyRemoteFolder(parentRemoteFolderId, folderName);

        // Populate is set because it is known that a new folder is empty
        remoteFolder.Populated = true;
        remoteFolder.Timestamp = GetNextTimestamp();
        remoteFolderCacheRepository.Insert(remoteFolder);
        return remoteFolder;
    }

    public override void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport)
    {
        Repository.DownloadFile(remoteFile, path, progressReport);
    }

    public override void TrashRemoteItem(string remoteItemId)
    {
        Repository.TrashRemoteItem(remoteItemId);

        // If item is in the file cache, remove it
        var remoteFile = remoteFileCacheRepository.SelectByKey(remoteItemId);
        if (remoteFileCacheRepository.DeleteByKey(remoteItemId) > 0)
        {
            return;
        }

        // If item is in the folder cache, clear all cache
        if (remoteFolderCacheRepository.SelectByKey(remoteItemId) != null)
        {
            ClearCache();
        }
    }

    public override RemoteItem RestoreRemoteItemFromTrash(string remoteItemId)
    {
        var remoteItem = Repository.RestoreRemoteItemFromTrash(remoteItemId);
        remoteItem.Timestamp = GetNextTimestamp();
        if (remoteItem is RemoteFile remoteFile) remoteFileCacheRepository.Upsert(remoteFile);
        else if (remoteItem is RemoteFolder remoteFolder)
        {
            remoteFolder.Populated = false;
            remoteFolderCacheRepository.Upsert(remoteFolder);
        }
        return remoteItem;
    }

    public override RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId)
    {
        var remoteItem = Repository.MoveRemoteItem(remoteItemId, parentRemoteFolderId);
        remoteItem.Timestamp = GetNextTimestamp();
        if (remoteItem is RemoteFile remoteFile)
        {
            remoteFileCacheRepository.Upsert(remoteFile);
        }
        if (remoteItem is RemoteFolder remoteFolder)
        {
            remoteFolder.Populated = false;
            remoteFolderCacheRepository.Upsert(remoteFolder);
        }
        return remoteItem;
    }

    public override RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        var remoteFolder = remoteFolderCacheRepository.SelectByKey(remoteFolderId);
        if (remoteFolder != null)
        {
            if (remoteFolder.Timestamp.IsExpired(cacheConfiguration.Ttl))
            {
                remoteFolderCacheRepository.DeleteByKey(remoteFolderId);
            }
            else if (remoteFolder.Populated)
            {
                remoteFiles = [.. remoteFileCacheRepository.SelectByFolderId(remoteFolderId)];
                remoteFolders = [.. remoteFolderCacheRepository.SelectByFolderId(remoteFolderId)];
                return remoteFolder;
            }
        }
        remoteFolder = Repository.GetRemoteFolder(remoteFolderId, out remoteFiles, out remoteFolders);
        remoteFolder.Populated = true;
        var timestamp = GetNextTimestamp();
        remoteFolder.Timestamp = timestamp;
        remoteFolderCacheRepository.Upsert(remoteFolder);
        foreach (var nestedRemoteFile in remoteFiles)
        {
            nestedRemoteFile.Timestamp = timestamp;
            remoteFileCacheRepository.Upsert(nestedRemoteFile);
        }
        foreach (var nestedRemoteFolder in remoteFolders)
        {
            nestedRemoteFolder.Timestamp = timestamp;
            nestedRemoteFolder.Populated = false;
            remoteFolderCacheRepository.Upsert(nestedRemoteFolder);
        }
        return remoteFolder;
    }

    public override RemoteItem GetRemoteItem(string remoteItemId)
    {
        var remoteFile = remoteFileCacheRepository.SelectByKey(remoteItemId);
        if (remoteFile != null)
        {
            if (!remoteFile.Timestamp.IsExpired(cacheConfiguration.Ttl))
            {
                return remoteFile;
            }
            remoteFileCacheRepository.DeleteByKey(remoteItemId);
        }
        else
        {
            var remoteFolder = remoteFolderCacheRepository.SelectByKey(remoteItemId);
            if (remoteFolder != null)
            {
                if (!remoteFolder.Timestamp.IsExpired(cacheConfiguration.Ttl))
                {
                    return remoteFolder;
                }
                remoteFolderCacheRepository.DeleteByKey(remoteItemId);
            }
        }
        var remoteItem = Repository.GetRemoteItem(remoteItemId);
        remoteItem.Timestamp = GetNextTimestamp();
        if (remoteItem is RemoteFile remoteFileToInsert) remoteFileCacheRepository.Insert(remoteFileToInsert);
        else if (remoteItem is RemoteFolder remoteFolderToInsert) remoteFolderCacheRepository.Insert(remoteFolderToInsert);
        return remoteItem;
    }

    public override void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        Repository.GetRemoteItemsInTrash(out remoteFiles, out remoteFolders);
    }

    public override void EmptyTrash() => Repository.EmptyTrash();
}
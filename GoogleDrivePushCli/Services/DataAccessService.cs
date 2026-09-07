using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using System;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Repositories;
using System.Collections.Generic;
using System.Linq;

namespace GoogleDrivePushCli.Services;

public class DataAccessService : DataAccessBase, IDisposable
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

    public static void Shutdown()
    {
        instance?.CloseConnection();
        instance = null;
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
        Defaults.EnsureConfigurationDirectory();
        connection = new SqliteConnection(Defaults.cacheDatabaseConnectionString);
        connection.Open();

        rootCacheRepository = new(connection);
        remoteFileCacheRepository = new(connection);
        remoteFolderCacheRepository = new(connection);

        EnsureSchema();

        if (!rootCacheRepository.IsInitialized)
        {
            rootCacheRepository.Model = new()
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                RootId = Repository.GetRootFolder().Id
            };
        }

        rootCache = rootCacheRepository.Model;
        if (CacheIsExpired)
        {
            ConsoleHelpers.Info($"TTL {cacheConfiguration.Ttl} met. Clearing cache.");
            ClearCache();
        }
    }

    private void EnsureSchema()
    {
        if (GetSchemaVersion() == Defaults.cacheSchemaVersion) return;
        ConsoleHelpers.Info("Cache schema is out of date. Rebuilding the cache.");
        DropTables();
        rootCacheRepository.CreateTable();
        remoteFileCacheRepository.CreateTable();
        remoteFolderCacheRepository.CreateTable();
        SetSchemaVersion(Defaults.cacheSchemaVersion);
    }

    private int GetSchemaVersion()
    {
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private void SetSchemaVersion(int version)
    {
        var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA user_version = {version}";
        command.ExecuteNonQuery();
    }

    private void DropTables()
    {
        var command = connection.CreateCommand();
        command.CommandText = string.Join(
            ' ',
            $"DROP TABLE IF EXISTS {nameof(RootCache)};",
            $"DROP TABLE IF EXISTS {nameof(RemoteFile)};",
            $"DROP TABLE IF EXISTS {nameof(RemoteFolder)};"
        );
        command.ExecuteNonQuery();
    }

    public override string RootId => rootCache.RootId;

    public override void CloseConnection()
    {
        connection.Close();
        connection.Dispose();
    }

    public void Dispose()
    {
        CloseConnection();
        GC.SuppressFinalize(this);
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

    private void InvalidateFolderListing(string remoteFolderId)
    {
        if (string.IsNullOrEmpty(remoteFolderId)) return;
        var remoteFolder = remoteFolderCacheRepository.SelectByKey(remoteFolderId);
        if (remoteFolder == null) return;
        remoteFolder.Populated = false;
        remoteFolderCacheRepository.Upsert(remoteFolder);
    }

    private void CacheRemoteItem(RemoteItem remoteItem)
    {
        remoteItem.Timestamp = GetNextTimestamp();
        if (remoteItem is RemoteFile remoteFile) remoteFileCacheRepository.Upsert(remoteFile);
        else if (remoteItem is RemoteFolder remoteFolder)
        {
            remoteFolder.Populated = false;
            remoteFolderCacheRepository.Upsert(remoteFolder);
        }
    }

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
        remoteFileCacheRepository.Upsert(remoteFile);
        return remoteFile;
    }

    public override RemoteFolder CreateEmptyRemoteFolder(string parentRemoteFolderId, string folderName)
    {
        var remoteFolder = Repository.CreateEmptyRemoteFolder(parentRemoteFolderId, folderName);
        remoteFolder.Populated = true;
        remoteFolder.Timestamp = GetNextTimestamp();
        remoteFolderCacheRepository.Upsert(remoteFolder);
        return remoteFolder;
    }

    public override void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport)
    {
        Repository.DownloadFile(remoteFile, path, progressReport);
    }

    public override void TrashRemoteItem(string remoteItemId)
    {
        var cachedFile = remoteFileCacheRepository.SelectByKey(remoteItemId);
        var cachedFolder = cachedFile == null ? remoteFolderCacheRepository.SelectByKey(remoteItemId) : null;
        Repository.TrashRemoteItem(remoteItemId);
        if (cachedFile != null)
        {
            remoteFileCacheRepository.DeleteByKey(remoteItemId);
            InvalidateFolderListing(cachedFile.FolderId);
            return;
        }
        if (cachedFolder != null) ClearCache();
    }

    public override RemoteItem RestoreRemoteItemFromTrash(string remoteItemId)
    {
        var remoteItem = Repository.RestoreRemoteItemFromTrash(remoteItemId);
        CacheRemoteItem(remoteItem);
        InvalidateFolderListing(remoteItem.FolderId);
        return remoteItem;
    }

    public override RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId)
    {
        var previousFolderId = GetCachedFolderId(remoteItemId);
        var remoteItem = Repository.MoveRemoteItem(remoteItemId, parentRemoteFolderId);
        CacheRemoteItem(remoteItem);
        InvalidateFolderListing(previousFolderId);
        InvalidateFolderListing(parentRemoteFolderId);
        return remoteItem;
    }

    public override RemoteItem RenameRemoteItem(string remoteItemId, string name)
    {
        var remoteItem = Repository.RenameRemoteItem(remoteItemId, name);
        CacheRemoteItem(remoteItem);
        InvalidateFolderListing(remoteItem.FolderId);
        return remoteItem;
    }

    public override RemoteFile CopyRemoteFile(string remoteFileId, string parentRemoteFolderId, string name)
    {
        var remoteFile = Repository.CopyRemoteFile(remoteFileId, parentRemoteFolderId, name);
        remoteFile.Timestamp = GetNextTimestamp();
        remoteFileCacheRepository.Upsert(remoteFile);
        InvalidateFolderListing(parentRemoteFolderId);
        return remoteFile;
    }

    private string GetCachedFolderId(string remoteItemId)
    {
        return remoteFileCacheRepository.SelectByKey(remoteItemId)?.FolderId ??
            remoteFolderCacheRepository.SelectByKey(remoteItemId)?.FolderId;
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
        PruneMissingChildren(remoteFolderId, remoteFiles, remoteFolders);
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

    private void PruneMissingChildren(string remoteFolderId, List<RemoteFile> remoteFiles, List<RemoteFolder> remoteFolders)
    {
        var currentFileIds = remoteFiles.Select(remoteFile => remoteFile.Id).ToHashSet();
        foreach (var cached in remoteFileCacheRepository.SelectByFolderId(remoteFolderId).ToList())
        {
            if (!currentFileIds.Contains(cached.Id)) remoteFileCacheRepository.DeleteByKey(cached.Id);
        }
        var currentFolderIds = remoteFolders.Select(remoteFolder => remoteFolder.Id).ToHashSet();
        foreach (var cached in remoteFolderCacheRepository.SelectByFolderId(remoteFolderId).ToList())
        {
            if (!currentFolderIds.Contains(cached.Id)) remoteFolderCacheRepository.DeleteByKey(cached.Id);
        }
    }

    public override RemoteItem GetRemoteItem(string remoteItemId)
    {
        var remoteFile = remoteFileCacheRepository.SelectByKey(remoteItemId);
        if (remoteFile != null)
        {
            if (!remoteFile.Timestamp.IsExpired(cacheConfiguration.Ttl)) return remoteFile;
            remoteFileCacheRepository.DeleteByKey(remoteItemId);
        }
        else
        {
            var remoteFolder = remoteFolderCacheRepository.SelectByKey(remoteItemId);
            if (remoteFolder != null)
            {
                if (!remoteFolder.Timestamp.IsExpired(cacheConfiguration.Ttl)) return remoteFolder;
                remoteFolderCacheRepository.DeleteByKey(remoteItemId);
            }
        }
        var remoteItem = Repository.GetRemoteItem(remoteItemId);
        remoteItem.Timestamp = GetNextTimestamp();
        if (remoteItem is RemoteFile remoteFileToCache) remoteFileCacheRepository.Upsert(remoteFileToCache);
        else if (remoteItem is RemoteFolder remoteFolderToCache) remoteFolderCacheRepository.Upsert(remoteFolderToCache);
        return remoteItem;
    }

    public override void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        Repository.GetRemoteItemsInTrash(out remoteFiles, out remoteFolders);
    }

    public override void EmptyTrash()
    {
        Repository.EmptyTrash();
        ClearCache();
    }
}

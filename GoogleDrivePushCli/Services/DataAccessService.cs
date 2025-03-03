using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using System;
using GoogleDrivePushCli.Json;
using GoogleDrivePushCli.Repositories;
using System.Collections.Generic;

namespace GoogleDrivePushCli.Services;

public class DataAccessManager : IDataAccessService
{
    private readonly RootCacheRepository rootCacheRepository;
    private readonly RemoteFileCacheRepository romoteFileCacheRepository;
    private readonly RemoteFolderCacheRepository remoteFolderCacheRepository;

    private readonly CacheConfiguration cacheConfiguration;
    private readonly RootCache rootCache;

    public DataAccessManager(CacheConfiguration cacheConfiguration, SqliteConnection connection)
    {
        rootCacheRepository = new(connection);
        romoteFileCacheRepository = new(connection);
        remoteFolderCacheRepository = new(connection);
        rootCache = rootCacheRepository.SelectByKey(1);
    }

    private long GetNextTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        rootCache.Timestamp = timestamp;
        rootCacheRepository.Update(rootCache);
        return timestamp;
    }

    private bool CacheIsExpired() => rootCache.Timestamp.IsExpired(cacheConfiguration.Ttl);

    public RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath)
    {
        throw new NotImplementedException();
    }

    public RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath)
    {
        throw new NotImplementedException();
    }

    public RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string folderName)
    {
        throw new NotImplementedException();
    }

    public void DownloadFile(string remoteFileId, string path)
    {
        throw new NotImplementedException();
    }

    public void TrashRemoteItem(string remoteItemId)
    {
        throw new NotImplementedException();
    }

    public void RestoreRemoteItemFromTrash(string remoteItemId)
    {
        throw new NotImplementedException();
    }

    public void MoveRemoteItem(string remoteItemId, string parentRemoteFolderId)
    {
        throw new NotImplementedException();
    }

    public RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        throw new NotImplementedException();
    }

    public RemoteItem GetRemoteItem(string itemId)
    {
        throw new NotImplementedException();
    }

    public void GetItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        throw new NotImplementedException();
    }

    public void EmptyTrash()
    {
        throw new NotImplementedException();
    }

    public Stack<RemoteItem> GetRemoteItemsFromPath(string path)
    {
        throw new NotImplementedException();
        // var stack = new Stack<RemoteItem>();
        // if (path.StartsWith(driveRoot)) path = path.ReplaceFirst(driveRoot, "/");
        // else if (!path.StartsWith('/')) path = $"/{path}";
        // var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p));
        // string currentId = rootIdAlias;
        // stack.Push(GetRem(rootIdAlias));
        // foreach (var part in parts)
        // {
        //     var items = GetItems(currentId, out var folder);
        //     var match = items.FirstOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
        //     if (match == default) throw new Exception($"No item matched for '{part}' in path '{path}'");
        //     currentId = match.Id;
        //     stack.Push(match);
        // }
        // return stack;
    }
}
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using System;
using GoogleDrivePushCli.Json;
using GoogleDrivePushCli.Repositories;

namespace GoogleDrivePushCli.Services;

public class DataAccessManager
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

    public long GetNextTimestamp()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        rootCache.Timestamp = timestamp;
        rootCacheRepository.Update(rootCache);
        return timestamp;
    }

    public bool CacheIsExpired() => rootCache.Timestamp.IsExpired(cacheConfiguration.Ttl);
}
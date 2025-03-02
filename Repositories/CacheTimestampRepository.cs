using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using GoogleDrivePushCli.Models.Schema;
using Microsoft.Data.Sqlite;
using System;
using GoogleDrivePushCli.Data;

namespace GoogleDrivePushCli.Repositories;

public class CacheTimestampRepository(
    Configuration configuration,
    SqliteConnection connection
) : Repository<CacheTimestamp>(
    new(
        nameof(CacheTimestamp),
        new("Singleton", PropertyType.String),
        [new(nameof(CacheTimestamp.Value), PropertyType.UtcDateTime, false)]
    ),
    connection
)
{
    public long GetNext()
    {
        var value = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var model = new CacheTimestamp()
        {
            Value = value
        };
        Update(model);
        return value;
    }

    public bool IsExpired()
    {
        var model = SelectByKey(CacheTimestamp.id);
        return model.Value.IsExpired(configuration.Cache.Ttl);
    }
}
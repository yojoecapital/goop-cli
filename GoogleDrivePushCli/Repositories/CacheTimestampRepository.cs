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
        new("Singleton", PropertyType.String, _ => 1, (_, _) => { }),
        [new(nameof(CacheTimestamp.Value), PropertyType.UtcDateTime)]
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
        var model = SelectByKey(1);
        return model.Value.IsExpired(configuration.Cache.Ttl);
    }
}
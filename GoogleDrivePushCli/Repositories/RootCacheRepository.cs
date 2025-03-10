using GoogleDrivePushCli.Models;
using Microsoft.Data.Sqlite;
using SqliteSchemaRepository;
using SqliteSchemaRepository.Schema;

namespace GoogleDrivePushCli.Repositories;

public class RootCacheRepository(SqliteConnection connection) : Repository<RootCache>(
    new(
        nameof(RootCache),
        new("Id", PropertyType.Integer, _ => 1, (_, _) => { }),
        [
            new(nameof(RootCache.RootId), PropertyType.String, false),
            new(nameof(RootCache.Timestamp), PropertyType.Long, false)
        ]
    ),
    connection
)
{
    public bool IsInitialized => Model != null;

    public RootCache Model
    {
        get => SelectByKey(1);
        set => Insert(value);
    }
}

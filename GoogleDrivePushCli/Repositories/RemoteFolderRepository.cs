using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Models.Schema;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Repositories;

public class RemoteFolderRepository(
    SqliteConnection connection
) : Repository<RemoteFolder>(
    new(
        nameof(RemoteFolder),
        new(nameof(RemoteFolder.Id), PropertyType.String),
        [
            new(nameof(RemoteFolder.Name), PropertyType.String),
            new(nameof(RemoteFolder.FolderId), PropertyType.String, true),
            new(nameof(RemoteFolder.Timestamp), PropertyType.Long)
        ]
    ),
    connection
)
{ }
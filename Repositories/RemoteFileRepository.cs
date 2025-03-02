using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Models.Schema;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Repositories;

public class RemoteFileRepository(
    SqliteConnection connection
) : Repository<RemoteFile>(
    new(
        nameof(RemoteFile),
        new(nameof(RemoteFile.Id), PropertyType.String),
        [
            new(nameof(RemoteFile.Name), PropertyType.String, false),
            new(nameof(RemoteFile.MimeType), PropertyType.String, false),
            new(nameof(RemoteFile.ModifiedTime), PropertyType.UtcDateTime, false),
            new(nameof(RemoteFile.Size), PropertyType.Long, false),
            new(nameof(RemoteFile.FolderId), PropertyType.String, false),
            new(nameof(RemoteFile.Timestamp), PropertyType.Long, false)
        ]
    ),
    connection
)
{ }
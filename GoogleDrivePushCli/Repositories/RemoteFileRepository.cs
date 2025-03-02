using GoogleDrivePushCli.Models;
using SqliteSchemaRepository;
using Microsoft.Data.Sqlite;
using SqliteSchemaRepository.Schema;

namespace GoogleDrivePushCli.Repositories;

public class RemoteFileRepository(
    SqliteConnection connection
) : Repository<RemoteFile>(
    new(
        nameof(RemoteFile),
        new(nameof(RemoteFile.Id), PropertyType.String),
        [
            new(nameof(RemoteFile.Name), PropertyType.String),
            new(nameof(RemoteFile.MimeType), PropertyType.String),
            new(nameof(RemoteFile.ModifiedTime), PropertyType.UtcDateTime),
            new(nameof(RemoteFile.Size), PropertyType.Long),
            new(nameof(RemoteFile.FolderId), PropertyType.String, true),
            new(nameof(RemoteFile.Timestamp), PropertyType.Long)
        ]
    ),
    connection
)
{ }
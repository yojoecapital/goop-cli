using GoogleDrivePushCli.Models;
using SqliteSchemaRepository;
using Microsoft.Data.Sqlite;
using SqliteSchemaRepository.Schema;
using System.Collections.Generic;

namespace GoogleDrivePushCli.Repositories;

public class RemoteFileCacheRepository(SqliteConnection connection) : Repository<RemoteFile>(
    new(
        nameof(RemoteFile),
        new(nameof(RemoteFile.Id), PropertyType.String),
        [
            new(nameof(RemoteFile.Name), PropertyType.String, false),
            new(nameof(RemoteFile.FolderId), PropertyType.String, true),
            new(nameof(RemoteFile.Timestamp), PropertyType.Long, false),
            new(nameof(RemoteFile.MimeType), PropertyType.String, false),
            new(nameof(RemoteFile.ModifiedTime), PropertyType.UtcDateTime, false),
            new(nameof(RemoteFile.Size), PropertyType.Long, false),
            new(nameof(RemoteFile.Trashed), PropertyType.Boolean, false)
        ]
    ),
    connection
)
{
    public IEnumerable<RemoteFile> SelectByFolderId(string folderId)
    {
        var command = Connection.CreateCommand();
        command.CommandText = ModelSchema.GetSelectByCommandText(
            $"{nameof(RemoteFile.FolderId)} = @{nameof(RemoteFile.FolderId)}"
        );
        command.Parameters.AddWithValue(nameof(RemoteFile.FolderId), folderId);
        var reader = command.ExecuteReader();
        while (reader.Read()) yield return ModelSchema.CreateModelFrom(reader);
    }

    public IEnumerable<RemoteFile> SelectByTrashed()
    {
        var command = Connection.CreateCommand();
        command.CommandText = ModelSchema.GetSelectByCommandText($"{nameof(RemoteFile.Trashed)} = 1");
        var reader = command.ExecuteReader();
        while (reader.Read()) yield return ModelSchema.CreateModelFrom(reader);
    }
}
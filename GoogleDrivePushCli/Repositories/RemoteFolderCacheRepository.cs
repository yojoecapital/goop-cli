using System.Collections.Generic;
using GoogleDrivePushCli.Json;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using SqliteSchemaRepository;
using SqliteSchemaRepository.Schema;

namespace GoogleDrivePushCli.Repositories;

public class RemoteFolderCacheRepository(SqliteConnection connection) : Repository<RemoteFolder>(
    new(
        nameof(RemoteFolder),
        new(nameof(RemoteFolder.Id), PropertyType.String),
        [
            new(nameof(RemoteFolder.Name), PropertyType.String, false),
            new(nameof(RemoteFolder.FolderId), PropertyType.String, true),
            new(nameof(RemoteFolder.Timestamp), PropertyType.Long, false),
            new(nameof(RemoteFolder.Populated), PropertyType.Boolean, false)
        ]
    ),
    connection
)
{
    public IEnumerable<RemoteFolder> SelectByFolderId(string folderId)
    {
        var command = Connection.CreateCommand();
        command.CommandText = ModelSchema.GetSelectByCommandText(
            $"{nameof(RemoteFolder.FolderId)} = @{nameof(RemoteFolder.FolderId)}"
        );
        command.Parameters.AddWithValue(nameof(RemoteFolder.FolderId), folderId);
        var reader = command.ExecuteReader();
        while (reader.Read()) yield return ModelSchema.CreateModelFrom(reader);
    }
}
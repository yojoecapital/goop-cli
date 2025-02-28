using System;
using System.Collections.Generic;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Data.Models;

public class CachedItemInFolder : RemoteItem
{
    public string FolderId { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Name)}, {nameof(MimeType)}, {nameof(ModifiedTime)}, {nameof(Size)}, {nameof(FolderId)}";

    private static CachedItemInFolder PopulateFrom(SqliteDataReader reader)
    {
        return new CachedItemInFolder()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            MimeType = reader.GetString(2),
            ModifiedTime = reader.IsDBNull(3) ?
                null :
                DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64(3)).UtcDateTime,
            Size = reader.IsDBNull(4) ?
                null :
                reader.GetInt64(4),
            FolderId = reader.GetString(5)
        };
    }

    public static void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(CachedItemInFolder)} (
                {nameof(Id)} VARCHAR PRIMARY KEY,
                {nameof(Name)} VARCHAR NOT NULL,
                {nameof(MimeType)} VARCHAR NOT NULL,
                {nameof(ModifiedTime)} INTEGER NULL,
                {nameof(Size)} INTEGER NULL,
                {nameof(FolderId)} INTEGER NOT NULL,
                FOREIGN KEY({nameof(FolderId)}) REFERENCES {nameof(CachedFolder)}({nameof(CachedFolder.Id)})
            );
            CREATE INDEX IF NOT EXISTS idx_{nameof(CachedItemInFolder)}_{nameof(FolderId)} ON {nameof(CachedItemInFolder)}({nameof(FolderId)});
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    private void Insert()
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            INSERT INTO {nameof(CachedItemInFolder)} ({propertiesString}) 
            VALUES (
                @{nameof(Id)}, @{nameof(Name)}, @{nameof(MimeType)}, @{nameof(ModifiedTime)}, 
                @{nameof(Size)}, @{nameof(FolderId)}
            );
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
        command.Parameters.AddWithValue($"@{nameof(Name)}", Name);
        command.Parameters.AddWithValue($"@{nameof(MimeType)}", MimeType);
        command.Parameters.AddWithValue($"@{nameof(ModifiedTime)}", ModifiedTime.HasValue ?
            new DateTimeOffset(ModifiedTime.Value).ToUnixTimeMilliseconds() :
            DBNull.Value
        );
        command.Parameters.AddWithValue($"@{nameof(Size)}", Size == null ? DBNull.Value : Size);
        command.Parameters.AddWithValue($"@{nameof(FolderId)}", FolderId);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static IEnumerable<CachedItemInFolder> SelectByFolderId(string folderId)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            SELECT {propertiesString} 
            FROM {nameof(CachedItemInFolder)} 
            WHERE {nameof(FolderId)} = @{nameof(FolderId)};
        ";
        command.Parameters.AddWithValue($"@{nameof(FolderId)}", folderId);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        using var reader = command.ExecuteReader();
        while (reader.Read()) yield return PopulateFrom(reader);
    }

    public static int DeleteByFolderId(string folderId)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(CachedItemInFolder)} 
            WHERE {nameof(FolderId)} = @{nameof(FolderId)};
        ";
        command.Parameters.AddWithValue($"@{nameof(FolderId)}", folderId);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected;
    }

    public static bool DeleteById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(CachedItemInFolder)} 
            WHERE {nameof(Id)} = @{nameof(Id)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", id);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public static void DeleteAll()
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(CachedItemInFolder)};
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
        ConsoleHelpers.Info("Cache cleared (items in folder).");
    }

    public static IEnumerable<CachedItemInFolder> InsertFrom(CachedFolder cachedFolder, IEnumerable<GoogleDriveFile> googleDriveFiles)
    {
        foreach (var googleDriveFile in googleDriveFiles)
        {
            var cachedItem = new CachedItemInFolder()
            {
                Id = googleDriveFile.Id,
                Name = googleDriveFile.Name,
                MimeType = googleDriveFile.MimeType,
                ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset.HasValue ?
                    googleDriveFile.ModifiedTimeDateTimeOffset.Value.DateTime :
                    null,
                Size = googleDriveFile.Size,
                FolderId = cachedFolder.Id
            };
            cachedItem.Insert();
            yield return cachedItem;
        }
    }
}
using System;
using System.Collections.Generic;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Data.Models;

public class CachedItemInFolder : RemoteItem
{
    public string FolderId { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Name)}, {nameof(MimeType)}, {nameof(ModifiedTime)}, {nameof(Size)}, {nameof(Trashed)}, {nameof(FolderId)}";

    private static CachedItemInFolder PopulateFrom(SqliteDataReader reader)
    {
        return new CachedItemInFolder()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            MimeType = reader.GetString(2),
            ModifiedTime = reader.GetDateTime(3),
            Size = reader.GetInt64(4),
            Trashed = reader.GetBoolean(5),
            FolderId = reader.GetString(6)
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
                {nameof(ModifiedTime)} INTEGER NOT NULL,
                {nameof(Size)} INTEGER NULL,
                {nameof(Trashed)} INTEGER NOT NULL,
                {nameof(FolderId)} INTEGER NOT NULL,
                FOREIGN KEY({nameof(FolderId)}) REFERENCES {nameof(CachedFolder)}({nameof(CachedFolder.Id)})
            );
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
                @{nameof(Size)}, @{nameof(Trashed)}, @{nameof(FolderId)}
            );
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
        command.Parameters.AddWithValue($"@{nameof(Name)}", Name);
        command.Parameters.AddWithValue($"@{nameof(MimeType)}", MimeType);
        command.Parameters.AddWithValue($"@{nameof(ModifiedTime)}", new DateTimeOffset(ModifiedTime).ToUnixTimeSeconds());
        command.Parameters.AddWithValue($"@{nameof(Size)}", Size);
        command.Parameters.AddWithValue($"@{nameof(Trashed)}", Trashed ? 1 : 0);
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
        ConsoleHelpers.Info("Cached items in folders cleared.");
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
                ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset.Value.DateTime,
                Size = googleDriveFile.Size,
                Trashed = googleDriveFile.Trashed.Value,
                FolderId = cachedFolder.Id
            };
            cachedItem.Insert();
            yield return cachedItem;
        }
    }
}
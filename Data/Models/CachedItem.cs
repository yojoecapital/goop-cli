using System;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Data.Models;

public class CachedItem : RemoteItem
{
    public long Timestamp { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Name)}, {nameof(MimeType)}, {nameof(ModifiedTime)}, {nameof(Size)}, {nameof(Trashed)}, {nameof(Timestamp)}";

    private static CachedItem PopulateFrom(SqliteDataReader reader)
    {
        return new CachedItem()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            MimeType = reader.GetString(2),
            ModifiedTime = reader.GetDateTime(3),
            Size = reader.GetInt64(4),
            Trashed = reader.GetBoolean(5),
            Timestamp = reader.GetInt64(6)
        };
    }

    public static void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(CachedItem)} (
                {nameof(Id)} VARCHAR PRIMARY KEY,
                {nameof(Name)} VARCHAR NOT NULL,
                {nameof(MimeType)} VARCHAR NOT NULL,
                {nameof(ModifiedTime)} INTEGER NOT NULL,
                {nameof(Size)} INTEGER NULL,
                {nameof(Trashed)} INTEGER NOT NULL,
                {nameof(Timestamp)} INTEGER NOT NULL
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
            INSERT INTO {nameof(CachedItem)} ({propertiesString}) 
            VALUES (
                @{nameof(Id)}, @{nameof(Name)}, @{nameof(MimeType)}, @{nameof(ModifiedTime)}, 
                @{nameof(Size)}, @{nameof(Trashed)}, @{nameof(Timestamp)}
            );
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
        command.Parameters.AddWithValue($"@{nameof(Name)}", Name);
        command.Parameters.AddWithValue($"@{nameof(MimeType)}", MimeType);
        command.Parameters.AddWithValue($"@{nameof(ModifiedTime)}", new DateTimeOffset(ModifiedTime).ToUnixTimeSeconds());
        command.Parameters.AddWithValue($"@{nameof(Size)}", Size);
        command.Parameters.AddWithValue($"@{nameof(Trashed)}", Trashed ? 1 : 0);
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", Timestamp);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static CachedItem SelectById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            SELECT {propertiesString} 
            FROM {nameof(CachedItem)} 
            WHERE {nameof(Id)} = @{nameof(Id)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", id);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var cachedItem = PopulateFrom(reader);
        return cachedItem;
    }

    public static bool DeleteById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(CachedItem)} 
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
            DELETE FROM {nameof(CachedItem)};
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
        ConsoleHelpers.Info("Cached items cleared.");
    }

    public bool IsExpired(long ttl)
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds() - Timestamp > ttl;
    }

    public bool IsExpired() => IsExpired(Defaults.ttl);

    public static CachedItem InsertFrom(GoogleDriveFile googleDriveFile)
    {
        var cachedItem = new CachedItem()
        {
            Id = googleDriveFile.Id,
            Name = googleDriveFile.Name,
            MimeType = googleDriveFile.MimeType,
            ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset.Value.DateTime,
            Size = googleDriveFile.Size,
            Trashed = googleDriveFile.Trashed.Value,
            Timestamp = CacheTimestamp.Get()
        };
        cachedItem.Insert();
        return cachedItem;
    }
}
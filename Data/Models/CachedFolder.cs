using System;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Data.Models;

public class CachedFolder
{
    public string Id { get; set; }
    public long Timestamp { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Timestamp)}";

    private static CachedFolder PopulateFrom(SqliteDataReader reader)
    {
        return new CachedFolder()
        {
            Id = reader.GetString(0),
            Timestamp = reader.GetInt64(1)
        };
    }

    public static void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(CachedFolder)} (
                {nameof(Id)} VARCHAR PRIMARY KEY,
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
            INSERT INTO {nameof(CachedFolder)} ({propertiesString}) 
            VALUES (@{nameof(Id)}, @{nameof(Timestamp)});
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", Timestamp);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static CachedFolder SelectById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            SELECT {propertiesString} 
            FROM {nameof(CachedFolder)} 
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
            DELETE FROM {nameof(CachedFolder)} 
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
            DELETE FROM {nameof(CachedFolder)};
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
        ConsoleHelpers.Info("Cache cleared (folders).");
    }


    public bool IsExpired(long ttl)
    {
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - Timestamp > ttl;
    }

    public bool IsExpired() => IsExpired(Defaults.ttl);

    public static CachedFolder InsertFrom(RemoteItem remoteFolder)
    {
        var cachedFolder = new CachedFolder()
        {
            Id = remoteFolder.Id,
            Timestamp = CacheTimestamp.Get()
        };
        cachedFolder.Insert();
        return cachedFolder;
    }
}
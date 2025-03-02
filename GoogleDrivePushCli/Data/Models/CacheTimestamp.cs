using System;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Data.Models;

public class CacheTimestamp
{
    public long Timestamp { get; set; }

    private static CacheTimestamp PopulateFrom(SqliteDataReader reader)
    {
        return new CacheTimestamp()
        {
            Timestamp = reader.GetInt64(0)
        };
    }

    public static void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(CacheTimestamp)} (
                Id INTEGER PRIMARY KEY CHECK (ID = 1),
                {nameof(Timestamp)} INTEGER NOT NULL
            );
            INSERT INTO {nameof(CacheTimestamp)} (Id, {nameof(Timestamp)}) 
            VALUES (1, @{nameof(Timestamp)});
        ";
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static long Get()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            UPDATE {nameof(CacheTimestamp)}
            SET {nameof(Timestamp)} = @{nameof(Timestamp)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", timestamp);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        var rowsAffected = command.ExecuteNonQuery();
        if (rowsAffected != 1)
        {
            throw new Exception("Fatal! Failed to update cache timestamp");
        }
        return timestamp;
    }

    public static bool IsExpired(long ttl)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            SELECT {nameof(Timestamp)} 
            FROM {nameof(CacheTimestamp)};
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new Exception("Fatal! The cache timestamp could not be read");
        var cacheTimestamp = PopulateFrom(reader);
        return DateTimeOffset.Now.ToUnixTimeMilliseconds() - cacheTimestamp.Timestamp > ttl;
    }

    public static bool IsExpired() => IsExpired(Defaults.ttl);
}
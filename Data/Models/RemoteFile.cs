using System;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Data.Models;

public class RemoteFile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string MimeType { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long Size { get; set; }
    public bool Trashed { get; set; }
    public DateTime Timestamp { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Name)}, {nameof(MimeType)}, {nameof(ModifiedTime)}, {nameof(Size)}, {nameof(Trashed)}, {nameof(Timestamp)}";

    private static RemoteFile PopulateFrom(SqliteDataReader reader)
    {
        return new RemoteFile()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            MimeType = reader.GetString(2),
            ModifiedTime = reader.GetDateTime(3),
            Size = reader.GetInt64(4),
            Trashed = reader.GetBoolean(5),
            Timestamp = reader.GetDateTime(6)
        };
    }

    public static void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(RemoteFile)} (
                {nameof(Id)} VARCHAR PRIMARY KEY,
                {nameof(Name)} VARCHAR NOT NULL,
                {nameof(MimeType)} VARCHAR NOT NULL,
                {nameof(ModifiedTime)} INTEGER NOT NULL,
                {nameof(Size)} INTEGER NOT NULL,
                {nameof(Trashed)} INTEGER NOT NULL,
                {nameof(Timestamp)} INTEGER NOT NULL
            );
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public void Insert()
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            INSERT INTO {nameof(RemoteFile)} ({propertiesString}) 
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
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", new DateTimeOffset(Timestamp).ToUnixTimeSeconds());
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static RemoteFile SelectById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            SELECT {propertiesString} 
            FROM {nameof(RemoteFile)} 
            WHERE {nameof(Id)} = @{nameof(Id)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", id);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        var remoteFile = PopulateFrom(reader);
        return remoteFile;
    }

    public static bool DeleteById(string id)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(RemoteFile)} 
            WHERE {nameof(Id)} = @{nameof(Id)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", id);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
    }

    public bool IsExpired(long ttl)
    {
        return DateTimeOffset.Now.ToUnixTimeSeconds() - new DateTimeOffset(Timestamp).ToUnixTimeSeconds() > ttl;
    }

    public bool IsExpired() => IsExpired(Defaults.ttl);
}
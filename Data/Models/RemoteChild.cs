using System;
using System.Collections.Generic;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Data.Models;

public class RemoteChild : RemoteFile
{
    public string Parent { get; set; }

    private static readonly string propertiesString = $"{nameof(Id)}, {nameof(Name)}, {nameof(MimeType)}, {nameof(ModifiedTime)}, {nameof(Size)}, {nameof(Trashed)}, {nameof(Parent)}, {nameof(Timestamp)}";

    private static RemoteChild PopulateFrom(SqliteDataReader reader)
    {
        return new RemoteChild()
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            MimeType = reader.GetString(2),
            ModifiedTime = reader.GetDateTime(3),
            Size = reader.GetInt64(4),
            Trashed = reader.GetBoolean(5),
            Parent = reader.GetString(6),
            Timestamp = reader.GetDateTime(7)
        };
    }

    public static new void CreateTable()
    {
        var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            CREATE TABLE IF NOT EXISTS {nameof(RemoteChild)} (
                {nameof(Id)} VARCHAR PRIMARY KEY,
                {nameof(Name)} VARCHAR NOT NULL,
                {nameof(MimeType)} VARCHAR NOT NULL,
                {nameof(ModifiedTime)} INTEGER NOT NULL,
                {nameof(Size)} INTEGER NOT NULL,
                {nameof(Trashed)} INTEGER NOT NULL,
                {nameof(Parent)} VARCHAR NULL,
                {nameof(Timestamp)} INTEGER NOT NULL
            );
        ";
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public new void Insert()
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            INSERT INTO {nameof(RemoteChild)} ({propertiesString}) 
            VALUES (
                @{nameof(Id)}, @{nameof(Name)}, @{nameof(MimeType)}, @{nameof(ModifiedTime)}, 
                @{nameof(Size)}, @{nameof(Trashed)}, @{nameof(Parent)}, @{nameof(Timestamp)}
            );
        ";
        command.Parameters.AddWithValue($"@{nameof(Id)}", Id);
        command.Parameters.AddWithValue($"@{nameof(Name)}", Name);
        command.Parameters.AddWithValue($"@{nameof(MimeType)}", MimeType);
        command.Parameters.AddWithValue($"@{nameof(ModifiedTime)}", new DateTimeOffset(ModifiedTime).ToUnixTimeSeconds());
        command.Parameters.AddWithValue($"@{nameof(Size)}", Size);
        command.Parameters.AddWithValue($"@{nameof(Trashed)}", Trashed ? 1 : 0);
        command.Parameters.AddWithValue($"@{nameof(Parent)}", Parent == null ? DBNull.Value : Parent);
        command.Parameters.AddWithValue($"@{nameof(Timestamp)}", new DateTimeOffset(Timestamp).ToUnixTimeSeconds());
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        command.ExecuteNonQuery();
    }

    public static IEnumerable<RemoteChild> SelectByParent(string parentId)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
        SELECT {propertiesString} 
        FROM {nameof(RemoteChild)} 
        WHERE {nameof(Parent)} = @{nameof(Parent)};";
        command.Parameters.AddWithValue($"@{nameof(Parent)}", parentId);

#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif

        using var reader = command.ExecuteReader();
        var files = new List<RemoteChild>();
        while (reader.Read()) yield return PopulateFrom(reader);
    }


    public static int DeleteByParent(string parentId)
    {
        using var command = ConnectionManager.Connection.CreateCommand();
        command.CommandText = @$"
            DELETE FROM {nameof(RemoteChild)} 
            WHERE {nameof(Parent)} = @{nameof(Parent)};
        ";
        command.Parameters.AddWithValue($"@{nameof(Parent)}", parentId);
#if DEBUG
        ConsoleHelpers.Info(command.CommandText);
#endif
        var rowsAffected = command.ExecuteNonQuery();
        return rowsAffected;
    }
}
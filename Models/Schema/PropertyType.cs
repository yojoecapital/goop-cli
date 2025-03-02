using System;
using Microsoft.Data.Sqlite;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Models.Schema;

public abstract class PropertyType(string sqlType)
{
    public readonly string sqlType = sqlType;

    public abstract object ReadValueFrom(SqliteDataReader reader, int ordinal);

    public static PropertyType String => new PropertyType<string>("VARCHAR", ReadString);
    public static PropertyType Integer => new PropertyType<int>("INTEGER", ReadInteger);
    public static PropertyType Long => new PropertyType<long>("INTEGER", ReadLong);
    public static PropertyType Boolean => new PropertyType<bool>("INTEGER", ReadBoolean);
    public static PropertyType UtcDateTime => new PropertyType<DateTime>("INTEGER", ReadUtcDateTime);

    private static string ReadString(SqliteDataReader reader, int ordinal) => reader.GetString(ordinal);
    private static int ReadInteger(SqliteDataReader reader, int ordinal) => reader.GetInt32(ordinal);
    private static long ReadLong(SqliteDataReader reader, int ordinal) => reader.GetInt64(ordinal);
    private static bool ReadBoolean(SqliteDataReader reader, int ordinal) => reader.GetBoolean(ordinal);
    private static DateTime ReadUtcDateTime(SqliteDataReader reader, int ordinal) => reader.GetInt64(ordinal).ToUtcDateTime();

}

public class PropertyType<T>(string sqlType, Func<SqliteDataReader, int, T> read) : PropertyType(sqlType)
{
    public readonly Func<SqliteDataReader, int, T> read = read;
    public override object ReadValueFrom(SqliteDataReader reader, int ordinal) => read(reader, ordinal);
}
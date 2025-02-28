using System.IO;
using GoogleDrivePushCli.Data.Models;
using GoogleDrivePushCli.Utilities;
using Microsoft.Data.Sqlite;

namespace GoogleDrivePushCli.Services;

public static class ConnectionManager
{
    private static SqliteConnection connection;
    public static SqliteConnection Connection
    {
        get
        {
            if (connection == null)
            {
                connection = new SqliteConnection($"Data Source={Defaults.cacheDatabasePath}");
                connection.Open();
                ConsoleHelpers.Info($"Connected to cache database at '{Defaults.cacheDatabasePath}'.");
            }
            return connection;
        }
    }

    public static void Close()
    {
        if (connection == null) return;
        connection.Close();
        connection.Dispose();
        ConsoleHelpers.Info($"Closed cache database connection.");
    }
}
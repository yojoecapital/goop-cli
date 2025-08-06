using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands;

public class ClearCache : Command
{
    public ClearCache() : base("clear-cache", "Removes the cache file.")
    {
        this.SetHandler(Handle);
    }

    private static void Handle()
    {
        if (File.Exists(Defaults.cacheDatabasePath))
        {
            File.Delete(Defaults.cacheDatabasePath);
            ConsoleHelpers.Info($"Removed '{Defaults.cacheDatabasePath}'.");
        }
        else ConsoleHelpers.Info($"No cache to clear.");
    }
}
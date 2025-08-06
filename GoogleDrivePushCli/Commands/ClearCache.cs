using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands;

public class ClearCache : Command
{
    public ClearCache() : base("clear-cache", "Removes the cache file.")
    {
        if (File.Exists(Defaults.cacheDatabasePath))
        {
            File.Delete(Defaults.cacheDatabasePath);
            Console.WriteLine($"Removed '{Defaults.cacheDatabasePath}'.");
        }
        else Console.WriteLine($"No cache to clear.");
    }
}
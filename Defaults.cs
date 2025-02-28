using System;
using System.IO;

namespace GoogleDrivePushCli
{
    public static class Defaults
    {
        public static readonly string applicationName = "Google Drive Push CLI";
        public static readonly string configurationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "goop-cli");
        public static readonly string metadataFileName = ".goop";
        public static readonly string ignoreListFileName = ".goopignore";
        public static readonly string credentialsPath = Path.Join(configurationPath, "credentials.json");
        public static readonly string tokensPath = Path.Join(configurationPath, "tokens");
        public static readonly string cacheDatabasePath = Path.Join(configurationPath, "cache.db");
        public static readonly long ttl = 5 * 60 * 1000;
    }
}
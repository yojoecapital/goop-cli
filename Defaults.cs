using System;
using System.IO;

namespace GoogleDrivePushCli
{
    public static class Defaults
    {
        public static readonly string applicationName = "Google Drive Push CLI";
        public static readonly string configurationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "goop-cli");
        public static readonly string credentialsFileName = "credentials.json";
        public static readonly string metadataFileName = ".goop";
        public static readonly string ignoreListFileName = ".goopignore";
        public static readonly string tokensDirectory = "tokens";
        public static readonly string cacheDirectory = "cache";
    }
}
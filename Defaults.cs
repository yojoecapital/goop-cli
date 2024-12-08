using System;
using System.IO;

namespace GoogleDrivePushCli
{
    internal static class Defaults
    {
        public static readonly string applicationName = "Google Drive Push CLI";
        public static readonly string configurationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "goop");
        public static readonly string credentialsFileName = "credentials.json";
        public static readonly string metadataFileName = ".goop";
        public static readonly string tokensDirectory = "tokens";
    }
}
using System;
using System.IO;

namespace GoogleDrivePushCli
{
    public static class Defaults
    {
        private const string applicationDirectoryName = "goop-cli";

        public static readonly string applicationName = "Google Drive Push CLI";
        public static readonly string configurationPath = ResolveConfigurationPath();
        public static readonly string configurationJsonPath = Path.Join(configurationPath, "config.json");
        public static readonly string linkTempalteFilePattern = "link-template.*";
        public static readonly string syncFolderFileName = ".goop";
        public static readonly string ignoreListFileName = ".goopignore";
        public static readonly string credentialsPath = Path.Join(configurationPath, "credentials.json");
        public static readonly string tokensPath = Path.Join(configurationPath, "tokens");
        public static readonly string cacheDatabasePath = Path.Join(configurationPath, "cache.db");
        public static readonly string cacheDatabaseConnectionString = $"Data Source={cacheDatabasePath}";
        public static readonly string rootIdAlias = "root";
        public static readonly string driveRoot = "My Drive";
        public static readonly long ttl = 5 * 60 * 1000;
        public static readonly int cacheSchemaVersion = 2;
        public static readonly int maxRootSearchDepth = 64;
        public static readonly int pageSize = 1000;

        public static void EnsureConfigurationDirectory() => Directory.CreateDirectory(configurationPath);

        private static string ResolveConfigurationPath()
        {
            var overridden = Environment.GetEnvironmentVariable("GOOP_CONFIG_HOME");
            if (IsUsable(overridden)) return Path.GetFullPath(overridden);

            var applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
            if (IsUsable(applicationData)) return Path.Join(applicationData, applicationDirectoryName);

            if (OperatingSystem.IsWindows())
            {
                var appData = Environment.GetEnvironmentVariable("APPDATA");
                if (IsUsable(appData)) return Path.Join(appData, applicationDirectoryName);
            }
            else
            {
                var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
                if (IsUsable(xdgConfigHome)) return Path.Join(xdgConfigHome, applicationDirectoryName);
            }

            var home = Environment.GetEnvironmentVariable("HOME") ??
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.Create);
            if (IsUsable(home)) return Path.Join(home, ".config", applicationDirectoryName);

            return Path.Join(Path.GetTempPath(), applicationDirectoryName);
        }

        private static bool IsUsable(string path) => !string.IsNullOrWhiteSpace(path) && Path.IsPathRooted(path);
    }
}

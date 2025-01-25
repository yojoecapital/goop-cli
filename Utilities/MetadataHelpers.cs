using System;
using System.IO;
using System.Text.Json;
using GoogleDrivePushCli.Data;

namespace GoogleDrivePushCli.Utilities
{
    public static class MetadataHelpers
    {
        public static string GetRootFolder(string workingDirectory)
        {
            try
            {
                var path = FindFileInParentDirectories(workingDirectory, Defaults.metadataFileName);
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return null;
            }
        }

        public static Metadata ReadMetadata(string workingDirectory, out string updatedWorkingDirectory)
        {
            var path = FindFileInParentDirectories(workingDirectory, Defaults.metadataFileName);
            updatedWorkingDirectory = Path.GetDirectoryName(path);
            Logger.Info($"Working directory is set to '{updatedWorkingDirectory}'.");
            try
            {
                return JsonSerializer.Deserialize(File.ReadAllText(path), MetadataJsonContext.Default.Metadata);
            }
            catch
            {
                throw new Exception($"The metadata file at '{path}' could not be parsed.");
            }
        }

        public static void WriteMetadata(Metadata metadata, string workingDirectory)
        {
            var path = Path.GetFullPath(Path.Join(workingDirectory, Defaults.metadataFileName));
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(metadata, MetadataJsonContext.Default.Metadata));
                Logger.Info($"Wrote metadata to '{path}'.");
            }
            catch
            {
                throw new Exception($"Could not write a metadata file at '{path}'.");
            }
        }

        public static string FindFileInParentDirectories(string path, string fileName)
        {
            int depth = 0;
            var current = new DirectoryInfo(path);
            while (current != null)
            {
                depth++;
                var filePath = Path.Combine(current.FullName, fileName);

                if (File.Exists(filePath)) return Path.GetFullPath(filePath);
                current = current.Parent;
            }
            throw new FileNotFoundException($"A metadata file could not be loaded from '{path}'.");
        }

    }
}
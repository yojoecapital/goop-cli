using System;
using System.IO;
using System.Text.Json;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static Metadata ReadMetadata(string workingDirectory, out string path)
        {
            path = FindFileInParentDirectories(workingDirectory, Defaults.metadataFileName);
            try
            {
                return JsonSerializer.Deserialize(File.ReadAllText(path), MetadataJsonContext.Default.Metadata);
            }
            catch
            {
                throw new Exception($"The metadata file at '{path}' could not be parsed.");
            }
        }

        private static void WriteMetadata(Metadata metadata, string workingDirectory)
        {
            var path = Path.Join(workingDirectory, Defaults.metadataFileName);
            try
            {
                File.WriteAllText(path, JsonSerializer.Serialize(metadata, MetadataJsonContext.Default.Metadata));
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

                if (File.Exists(filePath)) return filePath;
                current = current.Parent;
            }
            throw new FileNotFoundException($"A metadata file could not be loaded from '{path}'.");
        }

    }
}
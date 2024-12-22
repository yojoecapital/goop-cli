using System;
using System.IO;
using System.Text.Json;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static Metadata ReadMetadata(string workingDirectory)
        {
            var path = Path.Join(workingDirectory, Defaults.metadataFileName);
            if (!File.Exists(path)) throw new FileNotFoundException($"A metadata file could not be loaded at '{path}'.");
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
    }
}
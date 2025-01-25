using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    public class Metadata
    {
        [JsonPropertyName("structure")]
        public FolderMetadata Structure { get; set; } = new FolderMetadata();
        [JsonPropertyName("depth")]
        public int Depth { get; set; } = 3;

        public int Total(string workingDirectory) => Math.Max(Count(), Count(workingDirectory));

        public int Count() => Count(Structure);

        private int Count(FolderMetadata folderMetadata, int depth = 0)
        {
            if (depth >= Depth) return 0;
            var count = folderMetadata.Mappings.Keys.Where(fileName => !folderMetadata.Ignore.Contains(fileName)).Count();
            foreach (var nestedMetadata in folderMetadata.Nests.Values) count += Count(nestedMetadata, depth + 1);
            return count;
        }

        public int Count(string directory) => Count(directory, Structure);

        private int Count(string directory, FolderMetadata folderMetadata, int depth = 0)
        {
            if (depth >= Depth) return 0;
            var files = Directory.GetFiles(directory);
            int count;
            if (folderMetadata == null) count = files.Length;
            else count = files.Where(file => !folderMetadata.Ignore.Contains(Path.GetFileName(file))).Count();
            if (folderMetadata == null) foreach (var subDirectory in Directory.GetDirectories(directory)) count += Count(subDirectory, null, depth + 1);
            else
            {
                foreach (var subDirectory in Directory.GetDirectories(directory))
                {
                    var name = Path.GetFileName(subDirectory);
                    if (!folderMetadata.Ignore.Contains(name))
                    {
                        if (folderMetadata.Nests.TryGetValue(name, out var nestedMetadata)) count += Count(subDirectory, nestedMetadata, depth + 1);
                        else count += Count(subDirectory, null, depth + 1);
                    }
                }
            }
            return count;
        }

    }
}
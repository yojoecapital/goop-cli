using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    public class FolderMetadata
    {
        [JsonPropertyName("mappings")]
        public Dictionary<string, FileMetadata> Mappings { get; set; } = [];
        [JsonPropertyName("nests")]
        public Dictionary<string, FolderMetadata> Nests { get; set; } = [];

        [JsonPropertyName("folderId")]
        public string FolderId { get; set; } = string.Empty;
        [JsonPropertyName("ignore")]
        public HashSet<string> Ignore { get; set; } = [];
    }
}
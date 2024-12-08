using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    public class Metadata
    {
        [JsonPropertyName("mappings")]
        public Dictionary<string, FileMetadata> Mappings { get; set; } = new();

        [JsonPropertyName("folderId")]
        public string FolderId { get; set; } = string.Empty;
    }
}
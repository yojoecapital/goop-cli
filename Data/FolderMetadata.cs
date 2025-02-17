using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    public class FolderMetadata
    {
        [JsonPropertyName("files")]
        public Dictionary<string, FileMetadata> Files { get; set; } = [];
        [JsonPropertyName("folders")]
        public Dictionary<string, FolderMetadata> Folders { get; set; } = [];

        [JsonPropertyName("folderId")]
        public string FolderId { get; set; } = string.Empty;
    }
}
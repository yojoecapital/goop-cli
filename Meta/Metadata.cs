using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    public class Metadata
    {
        [JsonPropertyName("structure")]
        public FolderMetadata Structure { get; set; } = new FolderMetadata();
        [JsonPropertyName("depth")]
        public int Depth { get; set; } = 3;


    }
}
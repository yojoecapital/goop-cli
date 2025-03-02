using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    public class Metadata
    {
        [JsonPropertyName("structure")]
        public FolderMetadata Structure { get; set; } = new FolderMetadata();
        [JsonPropertyName("depth")]
        public int Depth { get; set; } = 3;
    }
}
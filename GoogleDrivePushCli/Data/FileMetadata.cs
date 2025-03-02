using System;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    public class FileMetadata
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("fileId")]
        public string FileId { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
    }
}
using System;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    public class FileMetadata
    {
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("fileId")]
        public string FileId { get; set; } = string.Empty;
    }
}
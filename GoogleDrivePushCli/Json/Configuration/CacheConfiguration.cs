using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.Configuration;

public class CacheConfiguration
{
    [JsonPropertyName("ttl")]
    public long Ttl { get; set; } = 5 * 60 * 1000;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

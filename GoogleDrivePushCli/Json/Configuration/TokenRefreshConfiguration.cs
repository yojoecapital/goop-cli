using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.Configuration;

public class TokenRefreshConfiguration
{
    [JsonPropertyName("max_token_retries")]
    public int MaxTokenRetries { get; set; } = 3;

    [JsonPropertyName("retry_delay")]
    public int RetryDelay { get; set; } = 1000;
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.Configuration;

[JsonSerializable(typeof(ApplicationConfiguration))]
[JsonSerializable(typeof(CacheConfiguration))]
[JsonSerializable(typeof(TokenRefreshConfiguration))]
public partial class ApplicationConfigurationJsonContext : JsonSerializerContext
{
    public static ApplicationConfigurationJsonContext Lenient => new(new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    });

    public static ApplicationConfigurationJsonContext Pretty => new(new() { WriteIndented = true });
}

using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.Configuration;

[JsonSerializable(typeof(ApplicationConfiguration))]
[JsonSerializable(typeof(CacheConfiguration))]
public partial class ApplicationConfigurationJsonContext : JsonSerializerContext
{
    public static ApplicationConfigurationJsonContext Pretty => new(new() { WriteIndented = true });
}

using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    [JsonSerializable(typeof(Configuration))]
    [JsonSerializable(typeof(CacheConfiguration))]
    public partial class ConfigurationJsonContext : JsonSerializerContext { }
}
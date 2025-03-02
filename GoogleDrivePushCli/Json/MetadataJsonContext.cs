using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json;

[JsonSerializable(typeof(Configuration))]
[JsonSerializable(typeof(CacheConfiguration))]
public partial class ConfigurationJsonContext : JsonSerializerContext { }

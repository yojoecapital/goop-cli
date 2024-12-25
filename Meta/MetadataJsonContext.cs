using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    [JsonSerializable(typeof(Metadata))]
    public partial class MetadataJsonContext : JsonSerializerContext { }
}
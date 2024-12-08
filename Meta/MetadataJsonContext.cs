using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    [JsonSerializable(typeof(FileMetadata))]
    [JsonSerializable(typeof(Dictionary<string, FileMetadata>))]
    [JsonSerializable(typeof(Metadata))]
    public partial class MetadataJsonContext : JsonSerializerContext { }
}
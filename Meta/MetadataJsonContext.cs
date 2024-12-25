using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    [JsonSerializable(typeof(FileMetadata))]
    [JsonSerializable(typeof(Dictionary<string, FileMetadata>))]
    [JsonSerializable(typeof(Dictionary<string, FolderMetadata>))]
    [JsonSerializable(typeof(Metadata))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(List<string>))]
    public partial class MetadataJsonContext : JsonSerializerContext { }
}
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Meta
{
    [JsonSerializable(typeof(Metadata))]
    [JsonSerializable(typeof(FolderMetadata))]
    [JsonSerializable(typeof(FileMetadata))]
    public partial class MetadataJsonContext : JsonSerializerContext { }
}
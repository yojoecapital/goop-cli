using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Data
{
    [JsonSerializable(typeof(Metadata))]
    [JsonSerializable(typeof(FolderMetadata))]
    [JsonSerializable(typeof(FileMetadata))]
    public partial class MetadataJsonContext : JsonSerializerContext { }
}
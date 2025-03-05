using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.SyncFolder;

[JsonSerializable(typeof(SyncFolder))]
public partial class SyncFolderJsonContext : JsonSerializerContext { }

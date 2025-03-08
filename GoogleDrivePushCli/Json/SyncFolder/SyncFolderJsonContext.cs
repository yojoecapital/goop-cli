using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.SyncFolder;

[JsonSerializable(typeof(SyncFolder))]
public partial class SyncFolderJsonContext : JsonSerializerContext
{
    public static SyncFolderJsonContext Pretty => new(new() { WriteIndented = true });
}
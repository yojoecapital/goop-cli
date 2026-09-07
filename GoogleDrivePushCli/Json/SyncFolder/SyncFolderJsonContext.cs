using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoogleDrivePushCli.Json.SyncFolder;

[JsonSerializable(typeof(SyncFolder))]
public partial class SyncFolderJsonContext : JsonSerializerContext
{
    public static SyncFolderJsonContext Lenient => new(new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    });

    public static SyncFolderJsonContext Pretty => new(new() { WriteIndented = true });
}

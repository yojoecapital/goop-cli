using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoogleDrivePushCli.Json.Configuration;

namespace GoogleDrivePushCli.Json.SyncFolder;

public class SyncFolder
{
    [JsonPropertyName("folder_id")]
    public string FolderId { get; set; }
    [JsonPropertyName("depth")]
    public int Depth { get; set; }
    [JsonIgnore]
    public string LocalDirectory { get; private set; }

    private static SyncFolder instance;
    public static SyncFolder Instance
    {
        get
        {
            instance ??= CreateSyncFolder();
            return instance;
        }
    }

    private static SyncFolder CreateSyncFolder()
    {
        var directory = FindParentDirectoryContainingFile(
            Directory.GetCurrentDirectory(),
            Defaults.syncFolderFileName,
            ApplicationConfiguration.Instance.MaxDepth
        );
        if (directory == null) return null;
        var syncFolderFilePath = Path.Join(directory, Defaults.syncFolderFileName);
        return JsonSerializer.Deserialize(
            File.ReadAllText(syncFolderFilePath),
            SyncFolderJsonContext.Default.SyncFolder
        );
    }

    static string FindParentDirectoryContainingFile(string startDirectory, string fileName, int maxDepth)
    {
        string currentDirectory = startDirectory;
        int depth = 0;
        while (currentDirectory != null && depth <= maxDepth)
        {
            string filePath = Path.Combine(currentDirectory, fileName);
            if (File.Exists(filePath))
            {
                return currentDirectory;
            }
            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            depth++;
        }
        return null;
    }
}
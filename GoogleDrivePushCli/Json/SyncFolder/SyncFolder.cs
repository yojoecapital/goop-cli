using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Services;

namespace GoogleDrivePushCli.Json.SyncFolder;

public class SyncFolder
{
    [JsonPropertyName("folder_id")]
    public string FolderId { get; set; }
    [JsonPropertyName("depth")]
    public int Depth { get; set; }
    [JsonIgnore]
    public string LocalDirectory { get; private set; }
    [JsonIgnore]
    public IgnoreListService IgnoreListService { get; private set; }

    public static SyncFolder Read(string workingDirectory)
    {
        var directory = FindParentDirectoryContainingFile(
            workingDirectory,
            Defaults.syncFolderFileName,
            ApplicationConfiguration.Instance.MaxDepth
        );
        if (directory == null) return null;
        var syncFolderFilePath = Path.Join(directory, Defaults.syncFolderFileName);
        var syncFolder = JsonSerializer.Deserialize(
            File.ReadAllText(syncFolderFilePath),
            SyncFolderJsonContext.Default.SyncFolder
        );
        syncFolder.LocalDirectory = directory;
        if (IgnoreListService.ExistsInDirectory(directory))
        {
            syncFolder.IgnoreListService = new IgnoreListService(directory);
        }
        return syncFolder;
    }

    public void Save(string workingDirectory)
    {
        var syncFolderFilePath = Path.Join(workingDirectory, Defaults.syncFolderFileName);
        var json = JsonSerializer.Serialize(this, SyncFolderJsonContext.Default.SyncFolder);
        File.WriteAllText(syncFolderFilePath, json);
    }

    public void Save() => Save(LocalDirectory);

    private static string FindParentDirectoryContainingFile(string startDirectory, string fileName, int maxDepth)
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
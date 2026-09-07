using System;
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
    public IgnoreList IgnoreList { get; private set; }

    [JsonIgnore]
    public int EffectiveDepth => Math.Min(Depth, ApplicationConfiguration.Instance.MaxDepth);

    public RelativePathFilter CreateFilter() => new(IgnoreList, LocalDirectory);

    public static SyncFolder Read(string workingDirectory)
    {
        var directory = FindRoot(workingDirectory) ??
            throw new FileNotFoundException($"No '{Defaults.syncFolderFileName}' file exists in '{Path.GetFullPath(workingDirectory)}' or any of its parent directories");
        var syncFolderFilePath = Path.Join(directory, Defaults.syncFolderFileName);
        SyncFolder syncFolder;
        try
        {
            syncFolder = JsonSerializer.Deserialize(
                File.ReadAllText(syncFolderFilePath),
                SyncFolderJsonContext.Lenient.SyncFolder
            );
        }
        catch (JsonException exception)
        {
            throw new Exception($"The sync folder file at '{syncFolderFilePath}' is not valid JSON: {exception.Message}");
        }
        if (syncFolder == null || string.IsNullOrEmpty(syncFolder.FolderId))
        {
            throw new Exception($"The sync folder file at '{syncFolderFilePath}' is missing a folder ID");
        }
        if (syncFolder.Depth <= 0) syncFolder.Depth = ApplicationConfiguration.Instance.DefaultDepth;
        syncFolder.LocalDirectory = directory;
        syncFolder.IgnoreList = new IgnoreList(directory);
        return syncFolder;
    }

    public void Save(string workingDirectory)
    {
        var syncFolderFilePath = Path.Join(workingDirectory, Defaults.syncFolderFileName);
        var json = JsonSerializer.Serialize(this, SyncFolderJsonContext.Pretty.SyncFolder) + Environment.NewLine;
        File.WriteAllText(syncFolderFilePath, json);
    }

    public void Save() => Save(LocalDirectory);

    public static string FindRoot(string startDirectory)
    {
        var currentDirectory = Path.GetFullPath(startDirectory);
        var depth = 0;
        while (currentDirectory != null && depth <= Defaults.maxRootSearchDepth)
        {
            if (File.Exists(Path.Join(currentDirectory, Defaults.syncFolderFileName))) return currentDirectory;
            currentDirectory = Directory.GetParent(currentDirectory)?.FullName;
            depth++;
        }
        return null;
    }
}

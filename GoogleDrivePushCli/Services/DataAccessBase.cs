using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Services;

public abstract class DataAccessBase
{
    public abstract RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath, IProgress<double> progressReport);

    public abstract RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath, IProgress<double> progressReport);

    public abstract RemoteFolder CreateEmptyRemoteFolder(string parentRemoteFolderId, string folderName);

    public RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string localFolderPath, int depth, IProgress<double> progressReport)
    {
        var totalFiles = FileManagementHelpers.CountFilesAtDepth(localFolderPath, depth, 0);
        var folderName = Path.GetFileName(localFolderPath);
        var remoteFolder = CreateEmptyRemoteFolder(parentRemoteFolderId, folderName);
        CreateRemoteFolder(
            remoteFolder, localFolderPath,
            0, depth,
            progressReport,
            new(), totalFiles
        );
        return remoteFolder;
    }

    private void CreateRemoteFolder(
        RemoteFolder parentRemoteFolder, string localFolderPath,
        int currentDepth, int depth,
        IProgress<double> progressReport,
        FileCounter fileCounter, int totalFiles
    )
    {
        if (currentDepth >= depth) return;
        foreach (var fileFullPath in Directory.GetFiles(localFolderPath))
        {
            var fileProgress = new Progress<double>(percent =>
            {
                var globalPercent = (fileCounter.Count + percent) / totalFiles;
                progressReport.Report(globalPercent);
            });
            CreateRemoteFile(parentRemoteFolder.Id, fileFullPath, fileProgress);
            fileCounter.Count++;
        }
        foreach (var folderFullPath in Directory.GetDirectories(localFolderPath))
        {
            var folderName = Path.GetFileName(folderFullPath);
            var nextRemoteFolder = CreateEmptyRemoteFolder(parentRemoteFolder.Id, folderName);
            CreateRemoteFolder(
                nextRemoteFolder, folderFullPath,
                currentDepth + 1, depth,
                progressReport,
                fileCounter, totalFiles
            );
        }
    }

    public abstract void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport);

    public void DownloadFolder(RemoteFolder remoteFolder, string path, int depth, IProgress<double> progressReport)
    {
        // Load all the files and folders into memory
        Dictionary<string, List<RemoteFile>> remoteFileMap = [];
        Dictionary<string, List<RemoteFolder>> remoteFolderMap = [];
        PopulateRemoteItemMaps(
            remoteFolder.Id, 0, depth,
            remoteFileMap, remoteFolderMap
        );
        int totalFiles = remoteFileMap.Values.Sum(list => list.Count);

        // Download each file recursively
        Directory.CreateDirectory(path);
        DownloadFolder(
            remoteFolder.Id, path, 0, depth,
            progressReport, new(), totalFiles,
            remoteFileMap, remoteFolderMap
        );
    }

    private void PopulateRemoteItemMaps(
        string remoteFolderId, int currentDepth, int depth,
        Dictionary<string, List<RemoteFile>> remoteFileMap,
        Dictionary<string, List<RemoteFolder>> remoteFolderMap
    )
    {
        if (currentDepth >= depth) return;
        GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        remoteFileMap[remoteFolderId] = remoteFiles;
        remoteFolderMap[remoteFolderId] = remoteFolders;
        foreach (var remoteFolder in remoteFolders) PopulateRemoteItemMaps(
            remoteFolder.Id, currentDepth + 1, depth,
            remoteFileMap, remoteFolderMap
        );
    }

    private void DownloadFolder(
        string remoteFolderId, string path, int currentDepth, int depth,
        IProgress<double> progressReport, FileCounter fileCounter, int totalFiles,
        Dictionary<string, List<RemoteFile>> remoteFileMap,
        Dictionary<string, List<RemoteFolder>> remoteFolderMap
    )
    {
        if (currentDepth >= depth) return;
        foreach (var remoteFile in remoteFileMap[remoteFolderId])
        {
            var filePath = Path.Join(path, remoteFile.Name);
            var fileProgress = new Progress<double>(percent =>
            {
                var globalPercent = (fileCounter.Count + percent) / totalFiles;
                progressReport.Report(globalPercent);
            });
            DownloadFile(remoteFile, filePath, fileProgress);
            fileCounter.Count++;
        }
        foreach (var remoteFolder in remoteFolderMap[remoteFolderId])
        {
            var folderPath = Path.Join(path, remoteFolder.Name);
            Directory.CreateDirectory(folderPath);
            DownloadFolder(
                remoteFolder.Id, folderPath, currentDepth + 1, depth,
                progressReport, fileCounter, totalFiles,
                remoteFileMap, remoteFolderMap
            );
        }
    }

    public abstract void TrashRemoteItem(string remoteItemId);

    public abstract RemoteItem RestoreRemoteItemFromTrash(string remoteItemId);

    public abstract RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId);

    public abstract RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public abstract RemoteItem GetRemoteItem(string remoteItemId);

    public abstract void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public abstract void EmptyTrash();

    public abstract string RootId { get; }

    public Stack<RemoteItem> GetRemoteItemsFromPath(string path) => GetRemoteItemsFromPath(path, RootId);

    public Stack<RemoteItem> GetRemoteItemsFromPath(string path, string startingId)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new Exception("Cannot process an empty path");
        var stack = new Stack<RemoteItem>();
        if (path.StartsWith(Defaults.driveRoot)) path = path.ReplaceFirst(Defaults.driveRoot, "/");
        else if (!path.StartsWith('/')) path = $"/{path}";
        var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p));
        string currentId = startingId;
        RemoteItem match = GetRemoteItem(startingId);
        foreach (var part in parts)
        {
            var remoteFolder = GetRemoteFolder(currentId, out var remoteFiles, out var remoteFolders);
            stack.Push(remoteFolder);
            match = (RemoteItem)remoteFolders
                .FirstOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase)) ??
                remoteFiles
                .FirstOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase)) ??
                throw new FileNotFoundException($"No item matched for '{part}' from the given path '{path}'");
            currentId = match.Id;
        }
        stack.Push(match);
        return stack;
    }

    public virtual void ClearCache() { }
    public virtual void CloseConnection() { }
}
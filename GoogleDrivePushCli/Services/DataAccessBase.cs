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

    public abstract RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string folderName);

    public abstract void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport);

    public void DownloadFolder(RemoteFolder remoteFolder, string path, int depth, IProgress<double> progressReport)
    {
        // Load all the files and folders into memory
        Dictionary<string, List<RemoteFile>> remoteFileMap = [];
        Dictionary<string, List<RemoteFolder>> remoteFolderMap = [];
        Stack<RemoteFolder> stack = new();
        stack.Push(remoteFolder);
        for (int currentDepth = 0; stack.Count > 0 && currentDepth < depth; currentDepth++)
        {
            var currentRemoteFolder = stack.Pop();
            GetRemoteFolder(currentRemoteFolder.Id, out var remoteFiles, out var remoteFolders);
            remoteFileMap[currentRemoteFolder.Id] = remoteFiles;
            remoteFolderMap[currentRemoteFolder.Id] = remoteFolders;
            foreach (var nextRemoteFolder in remoteFolders) stack.Push(nextRemoteFolder);
        }
        int totalFiles = remoteFileMap.Values.Sum(list => list.Count);

        // Download each file recursively
        Directory.CreateDirectory(path);
        DownloadFolder(
            remoteFolder.Id, path, 0, depth,
            progressReport, totalFiles,
            remoteFileMap, remoteFolderMap
        );
    }

    private void DownloadFolder(
        string remoteFolderId, string path, int depth, int maxDepth,
        IProgress<double> progressReport, int totalFiles,
        Dictionary<string, List<RemoteFile>> remoteFileMap,
        Dictionary<string, List<RemoteFolder>> remoteFolderMap
    )
    {
        if (depth >= maxDepth) return;
        foreach (var remoteFile in remoteFileMap[remoteFolderId])
        {
            var filePath = Path.Join(path, remoteFile.Name);
            var fileProgress = new Progress<double>(percent =>
            {
                var globalPercent = (totalFiles + percent) / totalFiles;
                progressReport.Report(globalPercent);
            });
            DownloadFile(remoteFile, filePath, fileProgress);
        }
        foreach (var remoteFolder in remoteFolderMap[remoteFolderId])
        {
            var folderPath = Path.Join(path, remoteFolder.Name);
            Directory.CreateDirectory(folderPath);
            DownloadFolder(
                remoteFolder.Id, folderPath, depth + 1, maxDepth,
                progressReport, totalFiles,
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
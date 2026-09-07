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

    public RemoteFolder CreateRemoteFolder(
        string parentRemoteFolderId,
        string localFolderPath,
        int depth,
        IProgress<double> progressReport,
        RelativePathFilter filter = null
    )
    {
        filter ??= RelativePathFilter.AllowAll;
        var totalFiles = CountUploadableFiles(localFolderPath, depth, 0, filter);
        var folderName = Path.GetFileName(localFolderPath);
        var remoteFolder = CreateEmptyRemoteFolder(parentRemoteFolderId, folderName);
        CreateRemoteFolder(
            remoteFolder, localFolderPath,
            0, depth,
            progressReport,
            new(), totalFiles,
            filter
        );
        progressReport.Report(1);
        return remoteFolder;
    }

    private static int CountUploadableFiles(string localFolderPath, int depth, int currentDepth, RelativePathFilter filter)
    {
        if (currentDepth >= depth) return 0;
        var total = 0;
        foreach (var fileFullPath in Directory.GetFiles(localFolderPath))
        {
            if (!filter.ShouldIgnore(fileFullPath)) total++;
        }
        foreach (var folderFullPath in Directory.GetDirectories(localFolderPath))
        {
            if (filter.ShouldIgnore(folderFullPath)) continue;
            total += CountUploadableFiles(folderFullPath, depth, currentDepth + 1, filter);
        }
        return total;
    }

    private void CreateRemoteFolder(
        RemoteFolder parentRemoteFolder, string localFolderPath,
        int currentDepth, int depth,
        IProgress<double> progressReport,
        FileCounter fileCounter, int totalFiles,
        RelativePathFilter filter
    )
    {
        if (currentDepth >= depth) return;
        foreach (var fileFullPath in Directory.GetFiles(localFolderPath))
        {
            if (filter.ShouldIgnore(fileFullPath))
            {
                ConsoleHelpers.Info($"Skipping local file '{fileFullPath}'.");
                continue;
            }
            CreateRemoteFile(parentRemoteFolder.Id, fileFullPath, ScaledProgress(progressReport, fileCounter, totalFiles));
            fileCounter.Count++;
        }
        foreach (var folderFullPath in Directory.GetDirectories(localFolderPath))
        {
            if (filter.ShouldIgnore(folderFullPath))
            {
                ConsoleHelpers.Info($"Skipping local folder '{folderFullPath}'.");
                continue;
            }
            var folderName = Path.GetFileName(folderFullPath);
            var nextRemoteFolder = CreateEmptyRemoteFolder(parentRemoteFolder.Id, folderName);
            CreateRemoteFolder(
                nextRemoteFolder, folderFullPath,
                currentDepth + 1, depth,
                progressReport,
                fileCounter, totalFiles,
                filter
            );
        }
    }

    private static IProgress<double> ScaledProgress(IProgress<double> progressReport, FileCounter fileCounter, int totalFiles)
    {
        if (totalFiles <= 0) return SynchronousProgress.None;
        var completed = fileCounter.Count;
        return new SynchronousProgress(percent => progressReport.Report(Math.Clamp((completed + percent) / totalFiles, 0, 1)));
    }

    public abstract void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport);

    public void DownloadFolder(
        RemoteFolder remoteFolder,
        string path,
        int depth,
        IProgress<double> progressReport,
        RelativePathFilter filter = null
    )
    {
        filter ??= RelativePathFilter.AllowAll;
        Dictionary<string, List<RemoteFile>> remoteFileMap = [];
        Dictionary<string, List<RemoteFolder>> remoteFolderMap = [];
        PopulateRemoteItemMaps(
            remoteFolder.Id, path, 0, depth,
            remoteFileMap, remoteFolderMap, filter
        );
        int totalFiles = remoteFileMap.Values.Sum(list => list.Count);
        Directory.CreateDirectory(path);
        DownloadFolder(
            remoteFolder.Id, path, 0, depth,
            progressReport, new(), totalFiles,
            remoteFileMap, remoteFolderMap, filter
        );
        progressReport.Report(1);
    }

    private void PopulateRemoteItemMaps(
        string remoteFolderId, string path, int currentDepth, int depth,
        Dictionary<string, List<RemoteFile>> remoteFileMap,
        Dictionary<string, List<RemoteFolder>> remoteFolderMap,
        RelativePathFilter filter
    )
    {
        if (currentDepth >= depth) return;
        GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        remoteFileMap[remoteFolderId] = [.. remoteFiles.Where(remoteFile => !filter.ShouldIgnore(Path.Join(path, remoteFile.Name)))];
        remoteFolderMap[remoteFolderId] = [.. remoteFolders.Where(nested => !filter.ShouldIgnore(Path.Join(path, nested.Name)))];
        foreach (var nested in remoteFolderMap[remoteFolderId]) PopulateRemoteItemMaps(
            nested.Id, Path.Join(path, nested.Name), currentDepth + 1, depth,
            remoteFileMap, remoteFolderMap, filter
        );
    }

    private void DownloadFolder(
        string remoteFolderId, string path, int currentDepth, int depth,
        IProgress<double> progressReport, FileCounter fileCounter, int totalFiles,
        Dictionary<string, List<RemoteFile>> remoteFileMap,
        Dictionary<string, List<RemoteFolder>> remoteFolderMap,
        RelativePathFilter filter
    )
    {
        if (currentDepth >= depth) return;
        foreach (var remoteFile in remoteFileMap[remoteFolderId])
        {
            var filePath = Path.Join(path, remoteFile.Name);
            DownloadFile(remoteFile, filePath, ScaledProgress(progressReport, fileCounter, totalFiles));
            fileCounter.Count++;
        }
        foreach (var remoteFolder in remoteFolderMap[remoteFolderId])
        {
            var folderPath = Path.Join(path, remoteFolder.Name);
            Directory.CreateDirectory(folderPath);
            DownloadFolder(
                remoteFolder.Id, folderPath, currentDepth + 1, depth,
                progressReport, fileCounter, totalFiles,
                remoteFileMap, remoteFolderMap, filter
            );
        }
    }

    public abstract void TrashRemoteItem(string remoteItemId);

    public abstract RemoteItem RestoreRemoteItemFromTrash(string remoteItemId);

    public abstract RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId);

    public abstract RemoteItem RenameRemoteItem(string remoteItemId, string name);

    public abstract RemoteFile CopyRemoteFile(string remoteFileId, string parentRemoteFolderId, string name);

    public RemoteItem CopyRemoteItem(RemoteItem remoteItem, string parentRemoteFolderId, string name, int depth, IProgress<double> progressReport)
    {
        if (remoteItem is RemoteFile remoteFile)
        {
            var copy = CopyRemoteFile(remoteFile.Id, parentRemoteFolderId, name ?? remoteFile.EffectiveRemoteName);
            progressReport.Report(1);
            return copy;
        }
        var totalFiles = CountRemoteFiles(remoteItem.Id, 0, depth);
        var remoteFolder = CopyRemoteFolder(remoteItem.Id, parentRemoteFolderId, name ?? remoteItem.EffectiveRemoteName, 0, depth, progressReport, new(), totalFiles);
        progressReport.Report(1);
        return remoteFolder;
    }

    private int CountRemoteFiles(string remoteFolderId, int currentDepth, int depth)
    {
        if (currentDepth >= depth) return 0;
        GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        return remoteFiles.Count + remoteFolders.Sum(nested => CountRemoteFiles(nested.Id, currentDepth + 1, depth));
    }

    private RemoteFolder CopyRemoteFolder(
        string remoteFolderId, string parentRemoteFolderId, string name,
        int currentDepth, int depth,
        IProgress<double> progressReport, FileCounter fileCounter, int totalFiles
    )
    {
        var copiedFolder = CreateEmptyRemoteFolder(parentRemoteFolderId, name);
        if (currentDepth >= depth) return copiedFolder;
        GetRemoteFolder(remoteFolderId, out var remoteFiles, out var remoteFolders);
        foreach (var remoteFile in remoteFiles)
        {
            CopyRemoteFile(remoteFile.Id, copiedFolder.Id, remoteFile.EffectiveRemoteName);
            fileCounter.Count++;
            if (totalFiles > 0) progressReport.Report(Math.Clamp(fileCounter.Count / (double)totalFiles, 0, 1));
        }
        foreach (var nested in remoteFolders)
        {
            CopyRemoteFolder(
                nested.Id, copiedFolder.Id, nested.EffectiveRemoteName,
                currentDepth + 1, depth,
                progressReport, fileCounter, totalFiles
            );
        }
        return copiedFolder;
    }

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
        path = StripDriveRootPrefix(path);
        var parts = path.Split('/').Where(part => !string.IsNullOrEmpty(part));
        string currentId = startingId;
        RemoteItem match = GetRemoteItem(startingId);
        foreach (var part in parts)
        {
            var remoteFolder = GetRemoteFolder(currentId, out var remoteFiles, out var remoteFolders);
            stack.Push(remoteFolder);
            match = MatchByName(remoteFolders, part) ??
                MatchByName(remoteFiles, part) ??
                throw new FileNotFoundException($"No item matched for '{part}' from the given path '{path}'");
            currentId = match.Id;
        }
        stack.Push(match);
        return stack;
    }

    private static RemoteItem MatchByName<T>(List<T> remoteItems, string part) where T : RemoteItem
    {
        return remoteItems.FirstOrDefault(remoteItem => remoteItem.Name.Equals(part, StringComparison.OrdinalIgnoreCase)) ??
            remoteItems.FirstOrDefault(remoteItem => remoteItem.EffectiveRemoteName.Equals(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string StripDriveRootPrefix(string path)
    {
        if (path.Equals(Defaults.driveRoot, StringComparison.OrdinalIgnoreCase)) return "/";
        if (path.StartsWith($"{Defaults.driveRoot}/", StringComparison.OrdinalIgnoreCase))
        {
            return path[Defaults.driveRoot.Length..];
        }
        return path.StartsWith('/') ? path : $"/{path}";
    }

    public virtual void ClearCache() { }
    public virtual void CloseConnection() { }
}

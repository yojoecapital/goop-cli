using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Services;

public interface IDataAccessService
{
    public RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath);

    public RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath);

    public RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string folderName);

    public void DownloadFile(string remoteFileId, string path);

    public void TrashRemoteItem(string remoteItemId);

    public RemoteItem RestoreRemoteItemFromTrash(string remoteItemId);

    public RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId);

    public RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public RemoteItem GetRemoteItem(string remoteItemId);

    public void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public void EmptyTrash();

    public string RootId { get; }

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
}
using System.Collections.Generic;
using GoogleDrivePushCli.Models;

namespace GoogleDrivePushCli.Services;

public interface IDataAccessService
{
    public RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath);

    public RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath);

    public RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string folderName);

    public void DownloadFile(string remoteFileId, string path);

    public void TrashRemoteItem(string remoteItemId);

    public void RestoreRemoteItemFromTrash(string remoteItemId);

    public void MoveRemoteItem(string remoteItemId, string parentRemoteFolderId);

    public RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public RemoteItem GetRemoteItem(string itemId);

    public void GetItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public void EmptyTrash();
}
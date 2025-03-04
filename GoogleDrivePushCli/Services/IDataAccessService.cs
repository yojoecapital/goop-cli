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

    public RemoteItem RestoreRemoteItemFromTrash(string remoteItemId);

    public RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId);

    public RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public RemoteItem GetRemoteItem(string remoteItemId);

    public void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders);

    public void EmptyTrash();
}
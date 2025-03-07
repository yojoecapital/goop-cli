using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Services;

public class DriveServiceWrapper : IDataAccessService
{
    private readonly DriveService service;
    private readonly UserCredential credential;
    private static readonly string defaultFolderFields = "id, name, trashed, parents";
    private static readonly string defaultFileFields = $"{defaultFolderFields}, mimeType, modifiedTime, size";
    private static readonly string[] driveScopes = [DriveService.Scope.Drive];

    public DriveServiceWrapper()
    {
        if (!File.Exists(Defaults.credentialsPath))
        {
            throw new Exception($"The credentials JSON could not be found at '{Defaults.credentialsPath}'");
        }

        // Get permission and make token
        try
        {
            using var stream = new FileStream(Defaults.credentialsPath, FileMode.Open, FileAccess.Read);
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                driveScopes,
                "user",
                CancellationToken.None,
                new FileDataStore(Defaults.tokensPath, true)
            ).Result;
        }
        catch (UnauthorizedAccessException)
        {
            throw new Exception($"Insufficient permissions");
        }
        catch (Google.GoogleApiException)
        {
            throw new Exception("Failed to authorize with Google API");
        }
        catch (Exception)
        {
            throw new Exception("Failed initialize Google Drive service");
        }

        // Try to refresh the token
        bool result = true;
        try
        {
            if (credential.Token.IsStale)
            {
                ConsoleHelpers.Info("Token expired, refreshing...");
                result = credential.RefreshTokenAsync(CancellationToken.None).Result;
            }
        }
        catch
        {
            throw new Exception("Failed refresh token");
        }
        if (result) ConsoleHelpers.Info("Token accepted.");
        else throw new Exception();

        // Create the service
        try
        {
            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = Defaults.applicationName,
            });
        }
        catch
        {
            throw new Exception("Failed to initialize Google Drive service");
        }
        ConsoleHelpers.Info(this);
    }

    public override string ToString()
    {
        try
        {
            var request = service.About.Get();
            request.Fields = "user";
            var about = request.Execute();
            return $"Established drive service for '{about.User.EmailAddress}'.";
        }
        catch
        {
            throw new Exception("Failed to query Google Drive");
        }
    }

    public RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath, IProgress<double> progressReport)
    {
        var body = service.Files.Get(remoteFileId).Execute();
        body.Id = null;
        body.Kind = null;
        body.Parents = null;
        using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        var totalSize = stream.Length;
        var request = service.Files.Update(body, remoteFileId, stream, "application/octet-stream");
        request.Fields = defaultFileFields;
        request.ProgressChanged += progress =>
        {
            if (progress.Status != UploadStatus.Uploading) return;
            var percentage = progress.BytesSent / (double)totalSize;
            progressReport?.Report(percentage);
        };
        var progress = request.Upload();
        if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to update remote file ({remoteFileId}) with content from '{localFilePath}'");
        ConsoleHelpers.Info($"Remote file ({remoteFileId}) has been updated successfully using '{localFilePath}'.");
        var remoteFile = RemoteFile.CreateFrom(request.ResponseBody);
        File.SetLastWriteTimeUtc(localFilePath, remoteFile.ModifiedTime);
        return remoteFile;
    }

    public RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath, IProgress<double> progressReport)
    {
        var body = new GoogleDriveFile()
        {
            Name = Path.GetFileName(localFilePath),
            Parents = [remoteFolderId]
        };
        using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        var totalSize = stream.Length;
        var request = service.Files.Create(body, stream, "application/octet-stream");
        request.Fields = defaultFileFields;
        request.ProgressChanged += progress =>
        {
            if (progress.Status != UploadStatus.Uploading) return;
            var percentage = progress.BytesSent / (double)totalSize;
            progressReport?.Report(percentage);
        };
        var progress = request.Upload();
        if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to upload '{localFilePath}' into remote folder ({remoteFolderId})");
        ConsoleHelpers.Info($"File '{localFilePath}' has been uploaded successfully into remote file ({request.ResponseBody.Id}).");
        var remoteFile = RemoteFile.CreateFrom(request.ResponseBody);
        File.SetLastWriteTimeUtc(localFilePath, remoteFile.ModifiedTime);
        return remoteFile;
    }

    public RemoteFolder CreateRemoteFolder(string parentRemoteFolderId, string folderName)
    {
        try
        {
            var body = new GoogleDriveFile()
            {
                Name = folderName,
                MimeType = RemoteFolder.MimeType,
                Parents = [parentRemoteFolderId]
            };
            var request = service.Files.Create(body);
            request.Fields = defaultFolderFields;
            var googleDriveFolder = request.Execute();
            ConsoleHelpers.Info($"Remote folder '{folderName}' ({googleDriveFolder.Id}) has been created successfully.");
            return RemoteFolder.CreateFrom(googleDriveFolder);
        }
        catch
        {
            throw new Exception($"Failed to create new folder '{folderName}' in remote folder ({parentRemoteFolderId})");
        }
    }

    public void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Create);
            var totalSize = remoteFile.Size;
            var request = service.Files.Get(remoteFile.Id);
            request.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status != DownloadStatus.Downloading) return;
                var percentage = progress.BytesDownloaded / (double)totalSize;
                progressReport?.Report(percentage);
            };
            request.Download(stream);
            File.SetLastWriteTimeUtc(path, remoteFile.ModifiedTime);
            ConsoleHelpers.Info($"Remote file ({remoteFile.Id}) has been successfully downloaded to '{path}'.");
        }
        catch (IOException)
        {
            throw new Exception($"Failed to save downloaded remote file ({remoteFile.Id}) due to an IO error");
        }
        catch (Exception)
        {
            throw new Exception($"Failed to download remote file ({remoteFile.Id}) to '{path}'");
        }
    }

    public void TrashRemoteItem(string remoteItemId)
    {
        try
        {
            var body = new GoogleDriveFile
            {
                Trashed = true
            };
            var request = service.Files.Update(body, remoteItemId);
            request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) has been trashed successfully.");
        }
        catch
        {
            throw new Exception($"Failed to trash remote item ({remoteItemId})");
        }
    }

    public RemoteItem RestoreRemoteItemFromTrash(string remoteItemId)
    {
        try
        {
            var body = new GoogleDriveFile
            {
                Trashed = false
            };
            var request = service.Files.Update(body, remoteItemId);
            request.Fields = defaultFileFields;
            var googleDriveItem = request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) has been trashed successfully.");
            if (googleDriveItem.MimeType == RemoteFolder.MimeType) return RemoteFolder.CreateFrom(googleDriveItem);
            else return RemoteFile.CreateFrom(googleDriveItem);
        }
        catch
        {
            throw new Exception($"Failed to trash remote item ({remoteItemId})");
        }
    }

    public RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId)
    {
        try
        {
            var request = service.Files.Update(null, remoteItemId);
            request.AddParents = parentRemoteFolderId;
            request.Fields = defaultFileFields;
            var googleDriveItem = request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) moved into remote folder ({parentRemoteFolderId}).");
            if (googleDriveItem.MimeType == RemoteFolder.MimeType) return RemoteFolder.CreateFrom(googleDriveItem);
            else return RemoteFile.CreateFrom(googleDriveItem);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception($"Failed to move  remote item ({remoteItemId}) into remote folder ({parentRemoteFolderId})");
        }
    }

    public RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        var remoteitem = GetRemoteItem(remoteFolderId);
        if (remoteitem is not RemoteFolder remoteFolder)
        {
            throw new Exception($"Remote item ({remoteFolderId}) is not a folder");
        }
        try
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"'{remoteFolderId}' in parents and trashed = false";
            listRequest.Fields = $"files({defaultFileFields})";
            var result = listRequest.Execute();
            remoteFiles = [];
            remoteFolders = [];
            foreach (var googleDriveItem in result.Files)
            {
                if (googleDriveItem.MimeType == RemoteFolder.MimeType) remoteFolders.Add(RemoteFolder.CreateFrom(googleDriveItem));
                else remoteFiles.Add(RemoteFile.CreateFrom(googleDriveItem));
            }
            return remoteFolder;
        }
        catch
        {
            throw new Exception($"Failed to fetch items for folder with ID ({remoteFolderId})");
        }
    }

    public void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        try
        {
            var listRequest = service.Files.List();
            listRequest.Q = $"trashed = true";
            listRequest.Fields = $"files({defaultFileFields})";
            var result = listRequest.Execute();
            remoteFiles = [];
            remoteFolders = [];
            foreach (var googleDriveItem in result.Files)
            {
                if (googleDriveItem.MimeType == RemoteFolder.MimeType) remoteFolders.Add(RemoteFolder.CreateFrom(googleDriveItem));
                else remoteFiles.Add(RemoteFile.CreateFrom(googleDriveItem));
            }
        }
        catch
        {
            throw new Exception($"Failed to fetch items in trash");
        }
    }

    public void EmptyTrash()
    {
        try
        {
            var emptyTrashRequest = service.Files.EmptyTrash();
            emptyTrashRequest.Execute();
        }
        catch
        {
            throw new Exception($"Failed to empty trash");
        }
    }

    public RemoteItem GetRemoteItem(string remoteItemId)
    {
        GoogleDriveFile googleDriveItem;
        try
        {
            var request = service.Files.Get(remoteItemId);
            request.Fields = defaultFileFields;
            googleDriveItem = request.Execute();
        }
        catch
        {
            throw new Exception($"Failed to fetch remote item ({remoteItemId})");
        }
        if (googleDriveItem.Trashed.Value) throw new Exception($"Remote item ({remoteItemId}) is trashed");
        if (googleDriveItem.MimeType == RemoteFolder.MimeType) return RemoteFolder.CreateFrom(googleDriveItem);
        else return RemoteFile.CreateFrom(googleDriveItem);
    }

    public RemoteFolder GetRootFolder() => (RemoteFolder)GetRemoteItem(Defaults.rootIdAlias);

    private string rootId;

    public string RootId
    {
        get
        {
            rootId ??= GetRootFolder().Id;
            return rootId;
        }
    }
}
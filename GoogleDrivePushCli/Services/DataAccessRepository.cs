using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using GoogleDrivePushCli.Json.Configuration;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Utilities;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Services;

public class DataAccessRepository : DataAccessBase
{
    private readonly DriveService service;
    private readonly UserCredential credential;
    private static readonly string defaultFolderFields = "id, name, trashed, parents, mimeType";
    private static readonly string defaultFileFields = $"{defaultFolderFields}, modifiedTime, size";
    private static readonly string[] driveScopes = [DriveService.Scope.Drive];

    public DataAccessRepository()
    {
        if (!File.Exists(Defaults.credentialsPath))
        {
            throw new Exception($"The credentials JSON could not be found at '{Defaults.credentialsPath}'");
        }
        credential = RetryHelper.Retry(
            GetUserCredential,
            ApplicationConfiguration.Instance.TokenRefreshConfiguration.MaxTokenRetries,
            ApplicationConfiguration.Instance.TokenRefreshConfiguration.RetryDelay
        );
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

    private UserCredential GetUserCredential()
    {
        ConsoleHelpers.Info("Getting user credentials...");
        UserCredential credential;
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
        else throw new Exception("Failed to refresh token");
        return credential;
    }

    public string GetAccountEmailAddress()
    {
        try
        {
            var request = service.About.Get();
            request.Fields = "user";
            return request.Execute().User.EmailAddress;
        }
        catch
        {
            throw new Exception("Failed to query Google Drive");
        }
    }

    public override string ToString() => $"Established drive service for '{GetAccountEmailAddress()}'.";

    private static void ReportUploadProgress(long totalSize, IProgress<double> progressReport, long bytesSent)
    {
        progressReport.Report(totalSize <= 0 ? 1 : Math.Clamp(bytesSent / (double)totalSize, 0, 1));
    }

    public override RemoteFile UpdateRemoteFile(string remoteFileId, string localFilePath, IProgress<double> progressReport)
    {
        var body = new GoogleDriveFile();
        using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        var totalSize = stream.Length;
        var request = service.Files.Update(body, remoteFileId, stream, "application/octet-stream");
        request.Fields = defaultFileFields;
        request.ProgressChanged += progress => ReportUploadProgress(totalSize, progressReport, progress.BytesSent);
        var progress = request.Upload();
        if (progress.Status == UploadStatus.Failed)
        {
            throw new Exception($"Failed to update remote file ({remoteFileId}) with content from '{localFilePath}': {progress.Exception?.Message}");
        }
        ConsoleHelpers.Info($"Remote file ({remoteFileId}) has been updated successfully using '{localFilePath}'.");
        var remoteFile = RemoteFile.CreateFrom(request.ResponseBody);
        File.SetLastWriteTimeUtc(localFilePath, remoteFile.ModifiedTime.ToUtcDateTime());
        return remoteFile;
    }

    public override RemoteFile CreateRemoteFile(string remoteFolderId, string localFilePath, IProgress<double> progressReport)
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
        request.ProgressChanged += progress => ReportUploadProgress(totalSize, progressReport, progress.BytesSent);
        var progress = request.Upload();
        if (progress.Status == UploadStatus.Failed)
        {
            throw new Exception($"Failed to upload '{localFilePath}' into remote folder ({remoteFolderId}): {progress.Exception?.Message}");
        }
        ConsoleHelpers.Info($"File '{localFilePath}' has been uploaded successfully into remote file ({request.ResponseBody.Id}).");
        var remoteFile = RemoteFile.CreateFrom(request.ResponseBody);
        File.SetLastWriteTimeUtc(localFilePath, remoteFile.ModifiedTime.ToUtcDateTime());
        return remoteFile;
    }

    public override RemoteFolder CreateEmptyRemoteFolder(string parentRemoteFolderId, string folderName)
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
        catch (Exception exception)
        {
            throw new Exception($"Failed to create new folder '{folderName}' in remote folder ({parentRemoteFolderId}): {exception.Message}");
        }
    }

    public override void DownloadFile(RemoteFile remoteFile, string path, IProgress<double> progressReport)
    {
        if (remoteFile.IsLink)
        {
            LinkFileHelper.CreateLinkFile(remoteFile.EffectiveRemoteName, remoteFile.WebViewLink, path);
            progressReport.Report(1);
            return;
        }
        try
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                var totalSize = remoteFile.Size;
                var request = service.Files.Get(remoteFile.Id);
                request.MediaDownloader.ProgressChanged += progress =>
                {
                    progressReport.Report(totalSize <= 0 ? 1 : Math.Clamp(progress.BytesDownloaded / (double)totalSize, 0, 1));
                };
                var download = request.DownloadWithStatus(stream);
                if (download.Status == DownloadStatus.Failed)
                {
                    throw new Exception(download.Exception?.Message ?? "the download did not complete");
                }
            }
            progressReport.Report(1);
            File.SetLastWriteTimeUtc(path, remoteFile.ModifiedTime.ToUtcDateTime());
            ConsoleHelpers.Info($"Remote file ({remoteFile.Id}) has been successfully downloaded to '{path}'.");
        }
        catch (IOException exception)
        {
            throw new Exception($"Failed to save downloaded remote file ({remoteFile.Id}) due to an IO error: {exception.Message}");
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to download remote file ({remoteFile.Id}) to '{path}': {exception.Message}");
        }
    }

    public override void TrashRemoteItem(string remoteItemId)
    {
        try
        {
            var request = service.Files.Update(new GoogleDriveFile { Trashed = true }, remoteItemId);
            request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) has been trashed successfully.");
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to trash remote item ({remoteItemId}): {exception.Message}");
        }
    }

    public override RemoteItem RestoreRemoteItemFromTrash(string remoteItemId)
    {
        try
        {
            var request = service.Files.Update(new GoogleDriveFile { Trashed = false }, remoteItemId);
            request.Fields = defaultFileFields;
            var googleDriveItem = request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) has been restored from the trash successfully.");
            return CreateRemoteItemFrom(googleDriveItem);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to restore remote item ({remoteItemId}) from the trash: {exception.Message}");
        }
    }

    public override RemoteItem MoveRemoteItem(string remoteItemId, string parentRemoteFolderId)
    {
        try
        {
            var currentParents = service.Files.Get(remoteItemId);
            currentParents.Fields = "parents";
            var existingParents = currentParents.Execute().Parents;
            var request = service.Files.Update(null, remoteItemId);
            request.AddParents = parentRemoteFolderId;
            if (existingParents != null && existingParents.Count > 0)
            {
                request.RemoveParents = string.Join(',', existingParents);
            }
            request.Fields = defaultFileFields;
            var googleDriveItem = request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) moved into remote folder ({parentRemoteFolderId}).");
            return CreateRemoteItemFrom(googleDriveItem);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to move remote item ({remoteItemId}) into remote folder ({parentRemoteFolderId}): {exception.Message}");
        }
    }

    public override RemoteItem RenameRemoteItem(string remoteItemId, string name)
    {
        try
        {
            var request = service.Files.Update(new GoogleDriveFile { Name = name }, remoteItemId);
            request.Fields = defaultFileFields;
            var googleDriveItem = request.Execute();
            ConsoleHelpers.Info($"Remote item ({remoteItemId}) has been renamed to '{name}'.");
            return CreateRemoteItemFrom(googleDriveItem);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to rename remote item ({remoteItemId}) to '{name}': {exception.Message}");
        }
    }

    public override RemoteFile CopyRemoteFile(string remoteFileId, string parentRemoteFolderId, string name)
    {
        try
        {
            var body = new GoogleDriveFile { Parents = [parentRemoteFolderId] };
            if (!string.IsNullOrEmpty(name)) body.Name = name;
            var request = service.Files.Copy(body, remoteFileId);
            request.Fields = defaultFileFields;
            var googleDriveFile = request.Execute();
            ConsoleHelpers.Info($"Remote file ({remoteFileId}) has been copied to ({googleDriveFile.Id}).");
            return RemoteFile.CreateFrom(googleDriveFile);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to copy remote file ({remoteFileId}) into remote folder ({parentRemoteFolderId}): {exception.Message}");
        }
    }

    private static RemoteItem CreateRemoteItemFrom(GoogleDriveFile googleDriveItem)
    {
        if (googleDriveItem.MimeType == RemoteFolder.MimeType) return RemoteFolder.CreateFrom(googleDriveItem);
        return RemoteFile.CreateFrom(googleDriveItem);
    }

    private void ListInto(string query, List<RemoteFile> remoteFiles, List<RemoteFolder> remoteFolders)
    {
        string pageToken = null;
        do
        {
            var listRequest = service.Files.List();
            listRequest.Q = query;
            listRequest.Fields = $"nextPageToken, files({defaultFileFields})";
            listRequest.PageSize = Defaults.pageSize;
            listRequest.PageToken = pageToken;
            var result = listRequest.Execute();
            if (result.Files != null)
            {
                foreach (var googleDriveItem in result.Files)
                {
                    if (googleDriveItem.MimeType == RemoteFolder.MimeType) remoteFolders.Add(RemoteFolder.CreateFrom(googleDriveItem));
                    else remoteFiles.Add(RemoteFile.CreateFrom(googleDriveItem));
                }
            }
            pageToken = result.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));
    }

    public override RemoteFolder GetRemoteFolder(string remoteFolderId, out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        var remoteItem = GetRemoteItem(remoteFolderId);
        if (remoteItem is not RemoteFolder remoteFolder)
        {
            throw new Exception($"Remote item ({remoteFolderId}) is not a folder");
        }
        remoteFiles = [];
        remoteFolders = [];
        try
        {
            ListInto($"'{remoteFolderId}' in parents and trashed = false", remoteFiles, remoteFolders);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to fetch items for folder with ID ({remoteFolderId}): {exception.Message}");
        }
        return remoteFolder;
    }

    public override void GetRemoteItemsInTrash(out List<RemoteFile> remoteFiles, out List<RemoteFolder> remoteFolders)
    {
        remoteFiles = [];
        remoteFolders = [];
        try
        {
            ListInto("trashed = true", remoteFiles, remoteFolders);
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to fetch items in trash: {exception.Message}");
        }
    }

    public override void EmptyTrash()
    {
        try
        {
            service.Files.EmptyTrash().Execute();
            ConsoleHelpers.Info("The trash has been emptied.");
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to empty trash: {exception.Message}");
        }
    }

    public override RemoteItem GetRemoteItem(string remoteItemId)
    {
        GoogleDriveFile googleDriveItem;
        try
        {
            var request = service.Files.Get(remoteItemId);
            request.Fields = defaultFileFields;
            googleDriveItem = request.Execute();
        }
        catch (Exception exception)
        {
            throw new Exception($"Failed to fetch remote item ({remoteItemId}): {exception.Message}");
        }
        if (googleDriveItem.Trashed == true) throw new Exception($"Remote item ({remoteItemId}) is trashed");
        return CreateRemoteItemFrom(googleDriveItem);
    }

    public RemoteFolder GetRootFolder() => (RemoteFolder)GetRemoteItem(Defaults.rootIdAlias);

    private string rootId;

    public override string RootId
    {
        get
        {
            rootId ??= GetRootFolder().Id;
            return rootId;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;

internal partial class Program
{
    private static readonly string applicationName = "Google Drive Push CLI";
    private static readonly string configurationPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "goop");
    private static readonly string credentialsFileName = "credentials.json";
    private static readonly string tokensDirectory = "tokens";
    private static readonly string[] driveScopes = [DriveService.Scope.Drive];
    private static bool verbose;
    private static DriveService service;

    private static void InitializeProgram(bool verbose)
    {
        // Set the verbose flag
        Program.verbose = verbose;

        // Ensure the configuration directory exists
        Directory.CreateDirectory(configurationPath);
        var credentialsPath = Path.Join(configurationPath, credentialsFileName);
        if (!File.Exists(credentialsPath))
        {
            throw new GoogleApiException($"A credentials JSON could not be found at '{credentialsPath}'.");
        }
        var tokensPath = Path.Join(configurationPath, tokensDirectory);
        UserCredential credential;

        // Get permission and make token
        using (var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.FromStream(stream).Secrets,
                driveScopes,
                "user",
                CancellationToken.None,
                new FileDataStore(tokensPath, true)
            ).Result;
        }

        // Create the service
        var service = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = applicationName,
        });

        // Test connection
        if (verbose)
        {
            var request = service.About.Get();
            request.Fields = "user";
            var about = request.Execute();
            WriteInfo($"Established drive service for {about.User.EmailAddress}.");
        }
        Program.service = service;
    }

    private static void UpdateFileInDrive(string fileId, string localFilePath)
    {
        var body = service.Files.Get(fileId).Execute();
        using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
        var request = service.Files.Update(body, fileId, fileStream, "application/octet-stream");
        request.ProgressChanged += progress =>
        {
            if (progress.Status == UploadStatus.Failed) throw new GoogleApiException($"Failed to upload the file. Status: {progress.Exception.Message}");
            else if (progress.Status == UploadStatus.Completed) WriteInfo($"File '{localFilePath}' ({fileId} has been updated successfully.");
        };
        var file = request.Upload();
        if (file.Status == UploadStatus.Completed) WriteInfo($"File '{localFilePath}' ({fileId}) has been uploaded successfully.");
    }

    private static void MoveFileToTrashInDrive(string fileId)
    {
        var body = new Google.Apis.Drive.v3.Data.File
        {
            Trashed = true
        };
        var request = service.Files.Update(body, fileId);
        request.Execute();
        WriteInfo($"File ({fileId}) has been moved to trash.");
    }

    private static string CreateFileInDrive(string folderId, string localFilePath)
    {
        var body = new Google.Apis.Drive.v3.Data.File()
        {
            Name = Path.GetFileName(localFilePath),
            Parents = [folderId] 
        };
        using var stream = new FileStream(localFilePath, FileMode.Open);
        var request = service.Files.Create(body, stream, "application/octet-stream");
        request.Fields = "id";
        request.ProgressChanged += progress =>
        {
            if (progress.Status == UploadStatus.Failed) throw new GoogleApiException($"Failed to upload the file. Status: {progress.Exception.Message}");
        };
        var file = request.Upload();
        if (file.Status == UploadStatus.Completed) WriteInfo($"File '{localFilePath}' ({request.ResponseBody.Id}) has been uploaded successfully.");
        return request.ResponseBody.Id;
    }

    private static IEnumerable<Google.Apis.Drive.v3.Data.File> GetFilesInDrive(string folderId)
    {
        try
        {
            var request = service.Files.List();
            request.Q = $"'{folderId}' in parents and trashed = false and mimeType != 'application/vnd.google-apps.folder'";
            request.Fields = "files(id, name, mimeType, modifiedTime)";
            var result = request.Execute();
            return result.Files;
        }
        catch
        {
            throw new GoogleApiException($"Failed to fetch files for folder ID '{folderId}'");
        }
    }

    private static void DownloadFileFromDrive(string workingDirectory, Google.Apis.Drive.v3.Data.File file)
    {
        var path = Path.Combine(workingDirectory, file.Name);
        using var stream = new FileStream(path, FileMode.Create);
        var getRequest = service.Files.Get(file.Id);
        getRequest.MediaDownloader.ProgressChanged += progress =>
        {
            if (progress.Status == Google.Apis.Download.DownloadStatus.Completed) WriteInfo($"Downloaded '{file.Name}'.");
        };
        getRequest.Download(stream);
    }
}
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

namespace GoogleDrivePushCli
{
    internal class DriveServiceWrapper
    {
       
        private readonly string[] driveScopes = [DriveService.Scope.Drive];
        private readonly DriveService service;

        public DriveServiceWrapper(string applicationName, string credentialsPath, string tokensPath)
        {
            if (!File.Exists(credentialsPath))
            {
                throw new GoogleApiException($"A credentials JSON could not be found at '{credentialsPath}'.");
            }
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
            service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName,
            });

            // Test connection
            if (Program.verbose)
            {
                var request = service.About.Get();
                request.Fields = "user";
                var about = request.Execute();
                Program.WriteInfo($"Established drive service for {about.User.EmailAddress}.");
            }
        }

        public void UpdateFile(string fileId, string localFilePath)
        {
            var body = service.Files.Get(fileId).Execute();
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            var request = service.Files.Update(body, fileId, fileStream, "application/octet-stream");
            request.ProgressChanged += progress =>
            {
                if (progress.Status == UploadStatus.Failed) throw new GoogleApiException($"Failed to upload the file. Status: {progress.Exception.Message}");
                else if (progress.Status == UploadStatus.Completed) Program.WriteInfo($"File '{localFilePath}' ({fileId} has been updated successfully.");
            };
            var file = request.Upload();
            if (file.Status == UploadStatus.Completed) Program.WriteInfo($"File '{localFilePath}' ({fileId}) has been uploaded successfully.");
        }

        public void MoveFileToTrash(string fileId)
        {
            var body = new Google.Apis.Drive.v3.Data.File
            {
                Trashed = true
            };
            var request = service.Files.Update(body, fileId);
            request.Execute();
            Program.WriteInfo($"File ({fileId}) has been moved to trash.");
        }

        public string CreateFile(string folderId, string localFilePath)
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
            if (file.Status == UploadStatus.Completed) Program.WriteInfo($"File '{localFilePath}' ({request.ResponseBody.Id}) has been uploaded successfully.");
            return request.ResponseBody.Id;
        }

        public IEnumerable<Google.Apis.Drive.v3.Data.File> GetFiles(string folderId)
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

        public string DownloadFile(string workingDirectory, Google.Apis.Drive.v3.Data.File file)
        {
            var path = Path.Combine(workingDirectory, file.Name);
            using var stream = new FileStream(path, FileMode.Create);
            var getRequest = service.Files.Get(file.Id);
            getRequest.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status == Google.Apis.Download.DownloadStatus.Completed) Program.WriteInfo($"Downloaded '{file.Name}'.");
            };
            getRequest.Download(stream);
            return path;
        }

        public string GetFolderName(string folderId)
        {
            // Ensure folder exists in Google Drive
            var folderRequest = service.Files.Get(folderId);
            folderRequest.Fields = "id, name, mimeType";
            Google.Apis.Drive.v3.Data.File folder;
            try
            {
                folder = folderRequest.Execute();
            }
            catch
            {
                throw new InvalidOperationException($"Folder with ID '{folderId}' does not exist.");
            }

            // Ensure the retrieved item is a file
            if (folder.MimeType != "application/vnd.google-apps.folder") throw new InvalidOperationException($"The provided ID '{folderId}' is not a folder.");
            return folder.Name;
        }
    }
}
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
        private static DriveServiceWrapper instance;

        public static DriveServiceWrapper Instance
        {
            get
            {
                instance ??= new DriveServiceWrapper();
                return instance;
            }
        }

        private DriveServiceWrapper()
        {
            var credentialsPath = Path.Join(Defaults.configurationPath, Defaults.credentialsFileName);
            if (!File.Exists(credentialsPath))
            {
                throw new GoogleApiException($"A credentials JSON could not be found at '{credentialsPath}'.");
            }
            UserCredential credential;

            // Get permission and make token
            var tokensPath = Path.Join(Defaults.configurationPath, Defaults.tokensDirectory);
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
                ApplicationName = Defaults.applicationName,
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

        public Google.Apis.Drive.v3.Data.File UpdateFile(string fileId, string localFilePath)
        {
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
            var body = service.Files.Get(fileId).Execute();
            body.Id = null;
            body.Kind = null;
            body.Parents = null;
            var request = service.Files.Update(body, fileId, fileStream, "application/octet-stream");
            request.Fields = "id, name, modifiedTime";
            request.ProgressChanged += progress =>
            {
                if (progress.Status == UploadStatus.Failed) throw new GoogleApiException($"Failed to upload the file. Status: {progress.Exception.Message}");
            };
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Completed) Program.WriteInfo($"File '{localFilePath}' ({fileId}) has been uploaded successfully.");
            return request.ResponseBody;
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

        public Google.Apis.Drive.v3.Data.File CreateFile(string folderId, string localFilePath)
        {
            var body = new Google.Apis.Drive.v3.Data.File()
            {
                Name = Path.GetFileName(localFilePath),
                Parents = [folderId] 
            };
            using var stream = new FileStream(localFilePath, FileMode.Open);
            var request = service.Files.Create(body, stream, "application/octet-stream");
            request.Fields = "id, name, modifiedTime";
            request.ProgressChanged += progress =>
            {
                if (progress.Status == UploadStatus.Failed) throw new GoogleApiException($"Failed to upload the file. Status: {progress.Exception.Message}");
            };
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Completed) Program.WriteInfo($"File '{localFilePath}' ({request.ResponseBody.Id}) has been uploaded successfully.");
            return request.ResponseBody;
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
                throw new GoogleApiException($"Failed to fetch files for folder ID '{folderId}'.");
            }
        }

        public string DownloadFile(string workingDirectory, Google.Apis.Drive.v3.Data.File file)
        {
            var path = Path.Combine(workingDirectory, file.Name);
            using var stream = new FileStream(path, FileMode.Create);
            var request = service.Files.Get(file.Id);
            request.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status == Google.Apis.Download.DownloadStatus.Completed) Program.WriteInfo($"Downloaded '{file.Name}'.");
            };
            request.Download(stream);
            return path;
        }

        public Google.Apis.Drive.v3.Data.File GetFile(string fileId)
        {
            // Ensure folder exists in Google Drive
            var request = service.Files.Get(fileId);
            request.Fields = "id, name, mimeType, modifiedTime";
            try
            {
                return request.Execute();
            }
            catch
            {
                throw new InvalidOperationException($"Remote item ('{fileId}') does not exist.");
            }
        }

        public string GetFolderName(string folderId)
        {
            // Ensure folder exists in Google Drive
            var folder = GetFile(folderId);

            // Ensure the retrieved item is a file
            if (folder.MimeType != "application/vnd.google-apps.folder") throw new InvalidOperationException($"The provided ID '{folderId}' is not a folder.");
            return folder.Name;
        }
    }
}
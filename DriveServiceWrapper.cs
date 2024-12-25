using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
        public static readonly string folderMimeType = "application/vnd.google-apps.folder";

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
                throw new Exception($"A credentials JSON could not be found at '{credentialsPath}'.");
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
            if (Logger.verbose)
            {
                var request = service.About.Get();
                request.Fields = "user";
                var about = request.Execute();
                Logger.Info($"Established drive service for {about.User.EmailAddress}.");
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
                if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to upload the file. Status: {progress.Exception.Message}");
            };
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Completed) Logger.Info($"File '{localFilePath}' ({fileId}) has been uploaded successfully.");
            return request.ResponseBody;
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
                if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to upload the file. Status: {progress.Exception.Message}");
            };
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Completed) Logger.Info($"File '{localFilePath}' ({request.ResponseBody.Id}) has been uploaded successfully.");
            return request.ResponseBody;
        }

        public string DownloadFile(string workingDirectory, Google.Apis.Drive.v3.Data.File file)
        {
            var path = Path.Combine(workingDirectory, file.Name);
            using var stream = new FileStream(path, FileMode.Create);
            var request = service.Files.Get(file.Id);
            request.MediaDownloader.ProgressChanged += progress =>
            {
                if (progress.Status == Google.Apis.Download.DownloadStatus.Completed) Logger.Info($"File '{file.Name}' ({file.Id}) has been downloaded successfullys.");
            };
            request.Download(stream);
            return path;
        }

        public void MoveFileToTrash(string fileId)
        {
            var body = new Google.Apis.Drive.v3.Data.File
            {
                Trashed = true
            };
            var request = service.Files.Update(body, fileId);
            request.Execute();
            Logger.Info($"File ({fileId}) has been trashed successfully.");
        }

        public IEnumerable<Google.Apis.Drive.v3.Data.File> GetItems(string folderId, out Google.Apis.Drive.v3.Data.File folder)
        {
            try
            {
                var request = service.Files.List();
                request.Q = $"('{folderId}' in parents or id = '{folderId}') and trashed = false";
                request.Fields = "files(id, name, mimeType, modifiedTime)";
                var result = request.Execute();

                // Extract folder name
                folder = result.Files.FirstOrDefault(
                    f => f.Id == folderId && f.MimeType == folderMimeType
                ) ?? throw new Exception($"A folder with ID '{folderId}' could not be found.");

                // Return children excluding the folder itself
                return result.Files.Where(f => f.Id != folderId);
            }
            catch
            {
                throw new Exception($"Failed to fetch items for a folder with an ID of '{folderId}'.");
            }
        }

        public Google.Apis.Drive.v3.Data.File GetItem(string fileId)
        {
            // Ensure folder exists in Google Drive
            var request = service.Files.Get(fileId);
            request.Fields = "id, name, mimeType, modifiedTime, trashed";
            try
            {
                var file = request.Execute();
                if (file.Trashed.HasValue && file.Trashed.Value) throw new Exception($"Remote item ('{fileId}') has been trashed.");
                return file;
            }
            catch
            {
                throw new Exception($"Remote item ('{fileId}') does not exist.");
            }
        }
    }
}
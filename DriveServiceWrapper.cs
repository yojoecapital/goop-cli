using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using GoogleDrivePushCli.Utilities;
using RemoteItem = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli
{
    public class DriveServiceWrapper
    {
        public static readonly string folderMimeType = "application/vnd.google-apps.folder";
        private readonly string driveRoot = "My Drive";
        private readonly string[] driveScopes = [DriveService.Scope.Drive];
        private readonly DriveService service;
        private readonly UserCredential credential;
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
                throw new Exception($"The credentials JSON could not be found at '{credentialsPath}'");
            }

            // Get permission and make token
            var tokensPath = Path.Join(Defaults.configurationPath, Defaults.tokensDirectory);
            try
            {
                using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    driveScopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(tokensPath, true)
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
                return $"Established drive service for {about.User.EmailAddress}.";
            }
            catch
            {
                throw new Exception("Failed to query Google Drive");
            }
        }

        public RemoteItem UpdateFile(string fileId, string localFilePath)
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
                if (progress.Status == UploadStatus.Failed) throw new Exception();
            };
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to update file '{localFilePath}' ({fileId})");
            ConsoleHelpers.Info($"File '{localFilePath}' ({fileId}) has been uploaded successfully.");
            return request.ResponseBody;
        }

        public RemoteItem UploadFile(string folderId, string localFilePath)
        {
            var body = new RemoteItem()
            {
                Name = Path.GetFileName(localFilePath),
                Parents = [folderId]
            };
            using var stream = new FileStream(localFilePath, FileMode.Open);
            var request = service.Files.Create(body, stream, "application/octet-stream");
            request.Fields = "id, name, modifiedTime";
            var progress = request.Upload();
            if (progress.Status == UploadStatus.Failed) throw new Exception($"Failed to upload file '{localFilePath}' into ({folderId})");
            ConsoleHelpers.Info($"File '{localFilePath}' ({request.ResponseBody.Id}) has been uploaded successfully.");
            return request.ResponseBody;
        }

        public RemoteItem CreateFolder(string parentFolderId, string folderName)
        {
            try
            {
                var body = new RemoteItem()
                {
                    Name = folderName,
                    MimeType = folderMimeType,
                    Parents = [parentFolderId]
                };
                var request = service.Files.Create(body);
                request.Fields = "id, name";
                var createdFolder = request.Execute();
                ConsoleHelpers.Info($"Folder '{folderName}' ({createdFolder.Id}) has been created successfully.");
                return createdFolder;
            }
            catch
            {
                throw new Exception($"Failed to create folder '{folderName}' in ({parentFolderId})");
            }
        }

        public string DownloadFile(string workingDirectory, RemoteItem file)
        {
            var path = Path.Combine(workingDirectory, file.Name);
            try
            {
                using var stream = new FileStream(path, FileMode.Create);
                var request = service.Files.Get(file.Id);
                request.MediaDownloader.ProgressChanged += progress =>
                {
                    if (progress.Status == Google.Apis.Download.DownloadStatus.Completed) ConsoleHelpers.Info($"File '{file.Name}' ({file.Id}) has been downloaded successfully.");
                };
                request.Download(stream);
                return path;
            }
            catch (IOException)
            {
                throw new Exception($"Failed to save downloaded file '{file.Name}' due to an IO error");
            }
            catch (Exception)
            {
                throw new Exception($"Failed to download '{file.Name}' into '{workingDirectory}'");
            }
            finally
            {
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception)
                    {
                        ConsoleHelpers.Error($"Failed to clean up partial file '{path}'");
                    }
                }
            }
        }

        public void MoveItemToTrash(string id)
        {
            try
            {
                var body = new RemoteItem
                {
                    Trashed = true
                };
                var request = service.Files.Update(body, id);
                request.Execute();
                ConsoleHelpers.Info($"Item ({id}) has been trashed successfully.");
            }
            catch
            {
                throw new Exception($"Failed to trash item with ID ({id})");
            }
        }

        private readonly Dictionary<string, (IEnumerable<RemoteItem> files, RemoteItem folder)> folderCache = [];

        public IEnumerable<RemoteItem> GetItems(string folderId, out RemoteItem folder)
        {
            if (folderCache.TryGetValue(folderId, out var value))
            {
                var cachedResult = value;
                folder = cachedResult.folder;
                return cachedResult.files;
            }
            try
            {
                folder = GetItem(folderId);
            }
            catch
            {
                throw new Exception($"Failed to fetch folder with ID ({folderId}).");
            }
            if (!IsFolder(folder))
            {
                throw new Exception($"The ID ({folderId}) does not correspond to a folder.");
            }
            try
            {
                var listRequest = service.Files.List();
                listRequest.Q = $"'{folderId}' in parents and trashed = false";
                listRequest.Fields = "files(id, name, mimeType, modifiedTime, size)";
                var result = listRequest.Execute();
                folderCache[folderId] = (result.Files, folder);
                return result.Files;
            }
            catch
            {
                throw new Exception($"Failed to fetch items for folder with ID ({folderId})");
            }
        }

        private readonly Dictionary<string, RemoteItem> itemCache = [];

        public RemoteItem GetItem(string id)
        {
            if (itemCache.TryGetValue(id, out var item)) return item;
            try
            {
                var request = service.Files.Get(id);
                request.Fields = "id, name, mimeType, modifiedTime, size, trashed";
                item = request.Execute();
            }
            catch
            {
                throw new Exception($"Failed to fetch item with ID ({id}).");
            }
            if (item.Trashed.HasValue && item.Trashed.Value)
            {
                throw new Exception($"Remote item ({id}) has been trashed");
            }
            itemCache[id] = item;
            return item;
        }

        private readonly Dictionary<string, RemoteItem> parentCache = [];

        public Stack<RemoteItem> GetItemsFromPath(string path)
        {
            var stack = new Stack<RemoteItem>();
            if (path.StartsWith(driveRoot)) path = path.ReplaceFirst(driveRoot, "/");
            else if (!path.StartsWith('/')) path = $"/{path}";
            var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p));
            string currentId = "root";
            stack.Push(GetItem("root"));
            foreach (var part in parts)
            {
                var items = GetItems(currentId, out var folder);
                var match = items.FirstOrDefault(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                if (match == default) throw new Exception($"No item matched for '{part}' in path '{path}'");
                currentId = match.Id;
                stack.Push(match);
            }
            return stack;
        }

        public static bool IsFolder(RemoteItem item) => item.MimeType == folderMimeType;
    }
}
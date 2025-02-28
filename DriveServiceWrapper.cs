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
using GoogleDrivePushCli.Data.Models;
using GoogleDrivePushCli.Utilities;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli
{
    public class DriveServiceWrapper
    {
        public static readonly string folderMimeType = "application/vnd.google-apps.folder";
        private static readonly string rootIdAlias = "root";
        private static readonly string trashIdAlias = "[TRASH]";
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
            if (!File.Exists(Defaults.cacheDatabasePath))
            {
                CacheTimestamp.CreateTable();
                CachedItem.CreateTable();
                CachedFolder.CreateTable();
                CachedItemInFolder.CreateTable();
            }
            else if (CacheTimestamp.IsExpired())
            {
                ConsoleHelpers.Info($"Cache TTL {Defaults.ttl} met.");
                CachedItemInFolder.DeleteAll();
                CachedFolder.DeleteAll();
                CachedItem.DeleteAll();
            }
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

        public GoogleDriveFile UpdateFile(string fileId, string localFilePath)
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

        public GoogleDriveFile UploadFile(string folderId, string localFilePath)
        {
            var body = new GoogleDriveFile()
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
            ClearCacheByItemId(folderId);
            return request.ResponseBody;
        }

        public GoogleDriveFile CreateFolder(string parentFolderId, string folderName)
        {
            try
            {
                var body = new GoogleDriveFile()
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
            finally
            {
                ClearCacheByItemId(parentFolderId);
            }
        }

        public string DownloadFile(GoogleDriveFile file, string path)
        {
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
                throw new Exception($"Failed to download '{file.Name}' to '{path}'");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        public void TrashItem(string id)
        {
            try
            {
                var body = new GoogleDriveFile
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
            finally
            {
                ClearCacheByItemId(id);
                ClearTrashCache();
            }
        }

        public GoogleDriveFile MoveItem(string itemId, string folderId)
        {
            if (!GetItem(folderId).IsFolder)
            {
                throw new Exception($"The ID ({folderId}) does not correspond to a folder");
            }
            try
            {
                var request = service.Files.Update(null, itemId);
                request.AddParents = folderId;
                request.Fields = "id, name, parents";
                var updatedItemResponse = request.Execute();
                ConsoleHelpers.Info($"Item ({itemId}) moved to folder ({folderId}).");
                return updatedItemResponse;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new Exception($"Failed to move item ({itemId}) to folder ({folderId})");
            }
            finally
            {
                ClearCacheByItemId(itemId);
                ClearCacheByItemId(folderId);
            }
        }

        public List<RemoteItem> GetItems(string folderId, out RemoteItem folder)
        {
            folder = GetItem(folderId);
            if (!folder.IsFolder) throw new Exception($"The ID ({folderId}) does not correspond to a folder");
            var cachedFolder = CachedFolder.SelectById(folderId);
            if (cachedFolder != null)
            {
                if (cachedFolder.IsExpired())
                {
                    CachedItemInFolder.DeleteByFolderId(folderId);
                    CachedFolder.DeleteById(folderId);
                }
                else return [.. CachedItemInFolder.SelectByFolderId(folderId).Cast<RemoteItem>()];
            }
            try
            {
                var listRequest = service.Files.List();
                listRequest.Q = $"'{folderId}' in parents and trashed = false";
                listRequest.Fields = "files(id, name, mimeType, modifiedTime, size)";
                var result = listRequest.Execute();
                cachedFolder = CachedFolder.InsertFrom(folder.Id);
                return [.. CachedItemInFolder.InsertFrom(cachedFolder, result.Files).Cast<RemoteItem>()];
            }
            catch
            {
                throw new Exception($"Failed to fetch items for folder with ID ({folderId})");
            }
        }

        public List<RemoteItem> GetItemsInTrash()
        {
            var trashCache = CachedFolder.SelectById(trashIdAlias);
            if (trashCache != null)
            {
                if (trashCache.IsExpired())
                {
                    CachedItemInFolder.DeleteByFolderId(trashIdAlias);
                    CachedFolder.DeleteById(trashIdAlias);
                }
                else return [.. CachedItemInFolder.SelectByFolderId(trashIdAlias).Cast<RemoteItem>()];
            }
            try
            {
                var listRequest = service.Files.List();
                listRequest.Q = $"trashed = true";
                listRequest.Fields = "files(id, name, mimeType, modifiedTime, size)";
                var result = listRequest.Execute();
                trashCache = CachedFolder.InsertFrom(trashIdAlias);
                return [.. CachedItemInFolder.InsertFrom(trashCache, result.Files).Cast<RemoteItem>()];
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
                ClearTrashCache();
            }
            catch
            {
                throw new Exception($"Failed to empty trash");
            }
        }

        private static void ClearTrashCache()
        {
            CachedItemInFolder.DeleteByFolderId(trashIdAlias);
            if (CachedFolder.DeleteById(trashIdAlias))
            {
                ConsoleHelpers.Info($"Cache cleared (trash).");
            }
        }

        private static void ClearCacheByItemId(string id)
        {
            if (CachedItem.DeleteById(id))
            {
                ConsoleHelpers.Info($"Item ({id}) was removed from cache (items).");
            }
            if (CachedItemInFolder.DeleteById(id))
            {
                ConsoleHelpers.Info($"Item ({id}) was removed from cache (items in folders).");
            }
            CachedItemInFolder.DeleteByFolderId(id);
            if (CachedFolder.DeleteById(id))
            {
                ConsoleHelpers.Info($"Item ({id}) was removed from cache (folders).");
            }
        }

        public bool IsRoot(RemoteItem item) => item.Id == GetItem(rootIdAlias).Id;

        public RemoteItem GetItem(string id)
        {
            var cachedItem = CachedItem.SelectById(id);
            if (cachedItem != null)
            {
                if (!cachedItem.IsExpired()) return cachedItem;
                else CachedItem.DeleteById(id);
            }
            GoogleDriveFile googleDriveFile;
            try
            {
                var request = service.Files.Get(id);
                request.Fields = "id, name, mimeType, modifiedTime, size, trashed";
                googleDriveFile = request.Execute();
            }
            catch
            {
                throw new Exception($"Failed to fetch item with ID ({id}).");
            }
            if (googleDriveFile.Trashed.Value)
            {
                throw new Exception($"Remote item ({id}) has been trashed");
            }

            // update the ID field here in case it is the root ID alias
            googleDriveFile.Id = id;
            return CachedItem.InsertFrom(googleDriveFile);
        }

        public Stack<RemoteItem> GetItemsFromPath(string path)
        {
            var stack = new Stack<RemoteItem>();
            if (path.StartsWith(driveRoot)) path = path.ReplaceFirst(driveRoot, "/");
            else if (!path.StartsWith('/')) path = $"/{path}";
            var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p));
            string currentId = rootIdAlias;
            stack.Push(GetItem(rootIdAlias));
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
    }
}
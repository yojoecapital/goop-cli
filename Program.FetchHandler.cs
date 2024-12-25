using System;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void FetchHandler(string workingDirectory, bool verbose)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory);
            var wasEdited = Fetch(metadata.Structure, metadata.Depth);

            // Update metadata
            if (wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Fetch complete.");
            }
            else Logger.Message("Up to date.");
        }

        private static bool Fetch(FolderMetadata folderMetadata, int maxDepth, int depth = 0)
        {
            var wasEdited = false;
            var remoteItems = DriveServiceWrapper.Instance.GetItems(folderMetadata.FolderId, out var folder);

            // Handle files
            var remoteFiles = remoteItems.Where(item => item.MimeType != DriveServiceWrapper.folderMimeType).ToDictionary(file => file.Id);
            Logger.Info($"{new string('.', depth)}Fetching remote data for folder '{folder.Name}' ({folder.Id}).");
            foreach (var pair in folderMetadata.Mappings)
            {
                Logger.Info($"Checking if remote file '{pair.Key}' was deleted.");
                if (!remoteFiles.ContainsKey(pair.Value.FileId))
                {
                    wasEdited = true;
                    folderMetadata.Mappings.Remove(pair.Key);
                    Logger.Info($"Remote file '{pair.Key}' was deleted.");
                }
            }
            foreach (var file in remoteFiles.Values)
            {
                if (folderMetadata.Mappings.TryGetValue(file.Name, out var fileMetadata))
                {
                    var timestamp = fileMetadata.Timestamp;
                    var remoteTimestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime;
                    Logger.Info($"Checking if remote file '{file.Name}' was updated by comparing remote time '{remoteTimestamp}' to cached remote time '{timestamp}' (>).");
                    if (remoteTimestamp > timestamp)
                    {
                        wasEdited = true;
                        fileMetadata.Timestamp = remoteTimestamp;
                        Logger.Info($"Remote file '{file.Name}' was updated from {timestamp} to {remoteTimestamp}.");
                    }
                }
                else
                {
                    wasEdited = true;
                    folderMetadata.Mappings[file.Name] = new()
                    {
                        Timestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime,
                        FileId = file.Id
                    };
                    Logger.Info($"Remote file '{file.Name}' was created.");
                }
            }

            // Handle folders
            if (depth < maxDepth)
            {
                var remoteFolders = remoteItems.Where(item => item.MimeType != DriveServiceWrapper.folderMimeType).ToDictionary(folder => folder.Id);

                foreach (var pair in folderMetadata.Nests)
                {
                    Logger.Info($"Checking if remote folder '{pair.Key}' was deleted.");
                    if (!remoteFolders.ContainsKey(pair.Value.FolderId))
                    {
                        wasEdited = true;
                        folderMetadata.Nests.Remove(pair.Key);
                        Logger.Info($"Remote folder '{pair.Key}' was deleted.");
                    }
                }
                foreach (var remoteFolder in remoteFolders.Values)
                {
                    Logger.Info($"Checking if remote folder '{remoteFolder.Name}' exists locally.");
                    if (!folderMetadata.Nests.ContainsKey(remoteFolder.Id))
                    {
                        wasEdited = true;
                        folderMetadata.Nests[remoteFolder.Name] = new()
                        {
                            FolderId = remoteFolder.Id
                        };
                        Logger.Info($"Remote folder '{remoteFolder.Name}' does not exist locally.");
                    }
                }

                // Handle nests
                var nestWasEdited = folderMetadata.Nests.Select(nest => Fetch(nest.Value, maxDepth, depth + 1))
                    .Aggregate(false, (current, result) => current || result); // Don't use Any because it will short circuit

                return wasEdited || nestWasEdited;
            }
            else if (folderMetadata.Nests.Count > 0)
            {
                // Trim off nests if the max depth was met
                wasEdited = true;
                folderMetadata.Nests = [];
            }
            return wasEdited;
        }
    }
}
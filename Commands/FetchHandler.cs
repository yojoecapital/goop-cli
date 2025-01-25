using System.Linq;
using GoogleDrivePushCli.Data;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands
{
    public static class FetchHandler
    {
        public static void Handle(string workingDirectory)
        {
            var metadata = MetadataHelpers.ReadMetadata(workingDirectory, out workingDirectory);
            (_, var wasEdited) = Fetch(metadata.Structure, workingDirectory, metadata.Depth, metadata.Total(workingDirectory));

            // Update metadata
            if (wasEdited)
            {
                MetadataHelpers.WriteMetadata(metadata, workingDirectory);
                Logger.Message("Fetch complete.");
            }
            else Logger.Message("Up to date.");
        }

        private static (int current, bool wasEdited) Fetch(
            FolderMetadata folderMetadata,
            string workingDirectory,
            int maxDepth,
            int total,
            int depth = 0,
            int current = 0
        )
        {
            var wasEdited = false;
            var remoteItems = DriveServiceWrapper.Instance.GetItems(folderMetadata.FolderId, out var folder);
            Logger.Info($"Stepping into '{folder.Name}'", depth);

            // Handle files
            var remoteFiles = remoteItems.Where(item => item.MimeType != DriveServiceWrapper.folderMimeType).ToDictionary(file => file.Id);
            Logger.Info($"Fetching remote data for folder '{folder.Name}' ({folder.Id}).", depth);
            foreach (var pair in folderMetadata.Mappings)
            {
                Logger.Info($"Checking if remote file '{pair.Key}' was deleted.", depth);
                if (!remoteFiles.ContainsKey(pair.Value.FileId))
                {
                    wasEdited = true;
                    folderMetadata.Mappings.Remove(pair.Key);
                    Logger.Info($"Remote file '{pair.Key}' was deleted.", depth);
                }
                Logger.Percent(++current, total);
            }
            foreach (var file in remoteFiles.Values)
            {
                if (folderMetadata.Mappings.TryGetValue(file.Name, out var fileMetadata))
                {
                    var timestamp = fileMetadata.Timestamp;
                    var remoteTimestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime;
                    Logger.Info($"Checking if remote file '{file.Name}' was updated by comparing remote time '{remoteTimestamp}' to cached remote time '{timestamp}' (>).", depth);
                    if (remoteTimestamp > timestamp)
                    {
                        wasEdited = true;
                        fileMetadata.Timestamp = remoteTimestamp;
                        Logger.Info($"Remote file '{file.Name}' was updated from {timestamp} to {remoteTimestamp}.", depth);
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
                    Logger.Info($"Remote file '{file.Name}' was created.", depth);
                }
                Logger.Percent(++current, total);
            }

            // Handle folders
            if (depth < maxDepth)
            {
                var remoteFolders = remoteItems.Where(item => item.MimeType == DriveServiceWrapper.folderMimeType).ToDictionary(folder => folder.Id);

                foreach (var pair in folderMetadata.Nests)
                {
                    Logger.Info($"Checking if remote folder '{pair.Key}' was deleted.", depth);
                    if (!remoteFolders.ContainsKey(pair.Value.FolderId))
                    {
                        wasEdited = true;
                        folderMetadata.Nests.Remove(pair.Key);
                        Logger.Info($"Remote folder '{pair.Key}' was deleted.", depth);
                    }
                }
                foreach (var remoteFolder in remoteFolders.Values)
                {
                    Logger.Info($"Checking if remote folder '{remoteFolder.Name}' is cached.", depth);
                    if (!folderMetadata.Nests.ContainsKey(remoteFolder.Name))
                    {
                        wasEdited = true;
                        folderMetadata.Nests[remoteFolder.Name] = new()
                        {
                            FolderId = remoteFolder.Id
                        };
                        Logger.Info($"Added remote folder '{remoteFolder.Name}' to cache.", depth);
                    }
                }

                // Handle nests
                foreach (var nest in folderMetadata.Nests)
                {
                    (current, var nestWasEdited) = Fetch(nest.Value, workingDirectory, maxDepth, total, depth + 1, current);
                    wasEdited |= nestWasEdited;
                }
            }
            else if (folderMetadata.Nests.Count > 0)
            {
                // Trim off nests if the max depth was met
                wasEdited = true;
                folderMetadata.Nests = [];
                Logger.Info($"Trimming of nested folders in cache at depth {depth}");
            }
            return (current, wasEdited);
        }
    }
}
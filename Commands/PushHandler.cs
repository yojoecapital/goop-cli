using System;
using System.IO;
using GoogleDrivePushCli.Data;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands
{
    public static class PushHandler
    {
        public static void Handle(string workingDirectory, bool noFetch, bool confirm)
        {
            if (!noFetch) FetchHandler.Handle(workingDirectory);
            var metadata = MetadataHelpers.ReadMetadata(workingDirectory, out workingDirectory);
            (_, var wasEdited) = Push(workingDirectory, workingDirectory, metadata.Structure, confirm, metadata.Depth, metadata.Total(workingDirectory));

            // Update metadata
            if (confirm && wasEdited)
            {
                MetadataHelpers.WriteMetadata(metadata, workingDirectory);
                Logger.Message("Push complete.");
            }
            if (!wasEdited) Logger.Message("Nothing to push.");
        }

        private static (int current, bool wasEdited) Push(
            string directory,
            string workingDirectory,
            FolderMetadata folderMetadata,
            bool confirm,
            int maxDepth,
            int total,
            int depth = 0,
            int current = 0
        )
        {
            var wasEdited = false;
            Logger.Info($"Checking to push into remote folder '{Path.GetFileName(directory)}'.", depth);
            var relativePath = Path.GetRelativePath(workingDirectory, directory);

            // Handle files
            foreach (string filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (folderMetadata.Ignore.Contains(fileName))
                {
                    Logger.Info($"Skipping local file '{Path.Join(relativePath, fileName)}'.", depth);
                    continue;
                }
                if (fileName == Defaults.metadataFileName) continue;
                if (folderMetadata.Mappings.TryGetValue(fileName, out var fileMetadata))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime <= fileMetadata.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Edit remote file '{Path.Join(relativePath, fileName)}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.UpdateFile(fileMetadata.FileId, filePath, depth);
                        var time = DateTime.UtcNow;
                        folderMetadata.Mappings[fileName].Timestamp = time;
                        File.SetLastWriteTimeUtc(filePath, time);
                        Logger.Info(message, depth);
                    }

                    else Logger.ToDo(message, depth);
                }
                else
                {
                    // The file was created
                    wasEdited = true;
                    var message = $"Create remote file '{Path.Join(relativePath, fileName)}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.CreateFile(folderMetadata.FolderId, filePath, depth);
                        var time = DateTime.UtcNow;
                        folderMetadata.Mappings[fileName] = new()
                        {
                            FileId = file.Id,
                            Timestamp = time
                        };
                        File.SetLastWriteTimeUtc(filePath, time);
                        Logger.Info(message, depth);
                    }
                    else Logger.ToDo(message, depth);
                }
                Logger.Percent(++current, total);
            }
            foreach (var pair in folderMetadata.Mappings)
            {
                if (folderMetadata.Ignore.Contains(pair.Key))
                {
                    Logger.Info($"Skipping cached remote file '{Path.Join(relativePath, pair.Key)}'.", depth);
                    continue;
                }
                var filePath = Path.Join(directory, pair.Key);
                if (!File.Exists(filePath))
                {
                    // The file was deleted
                    wasEdited = true;
                    var message = $"Delete remote file '{Path.Join(relativePath, pair.Key)}'.";
                    if (confirm)
                    {
                        DriveServiceWrapper.Instance.MoveItemToTrash(pair.Value.FileId, depth);
                        folderMetadata.Mappings.Remove(pair.Key);
                        Logger.Info(message, depth);
                    }
                    else Logger.ToDo(message, depth);
                }
                Logger.Percent(++current, total);
            }

            // Handle folders
            if (depth < maxDepth)
            {
                foreach (string folderPath in Directory.GetDirectories(directory))
                {
                    var folderName = Path.GetFileName(folderPath);
                    if (folderMetadata.Ignore.Contains(folderName))
                    {
                        Logger.Info($"Skipping local folder '{Path.Join(relativePath, folderName)}'.", depth);
                        continue;
                    }
                    if (!folderMetadata.Nests.TryGetValue(folderName, out var nestedMetadata))
                    {
                        nestedMetadata = new();

                        // The folder was created
                        wasEdited = true;
                        var message = $"Create remote folder '{Path.Join(relativePath, folderName)}'.";
                        if (confirm)
                        {
                            var folder = DriveServiceWrapper.Instance.CreateFolder(folderMetadata.FolderId, folderName, depth);
                            nestedMetadata.FolderId = folder.Id;
                            Logger.Info(message, depth);
                        }
                        else Logger.ToDo(message, depth);
                        folderMetadata.Nests[folderName] = nestedMetadata;
                    }
                }
                foreach (var pair in folderMetadata.Nests)
                {
                    if (folderMetadata.Ignore.Contains(pair.Key))
                    {
                        Logger.Info($"Skipping cached remote folder '{Path.Join(relativePath, pair.Key)}'.", depth);
                        continue;
                    }
                    var folderPath = Path.Join(directory, pair.Key);
                    if (Directory.Exists(folderPath))
                    {
                        (current, var nestWasEdited) = Push(Path.Join(directory, pair.Key), workingDirectory, pair.Value, confirm, maxDepth, total, depth + 1, current);
                        wasEdited |= nestWasEdited;
                    }
                    else
                    {
                        // The folder was deleted
                        wasEdited = true;
                        var message = $"Delete remote folder '{Path.Join(relativePath, pair.Key)}'.";
                        if (confirm)
                        {
                            DriveServiceWrapper.Instance.MoveItemToTrash(pair.Value.FolderId, depth);
                            folderMetadata.Mappings.Remove(pair.Key);
                            Logger.Info(message, depth);
                        }
                        else Logger.ToDo(message, depth);
                    }
                }
            }
            return (current, wasEdited);
        }
    }
}
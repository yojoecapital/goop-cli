using System;
using System.IO;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void PushHandler(string workingDirectory, bool verbose, bool confirm)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory);
            var wasEdited = Push(workingDirectory, metadata.Structure, confirm, metadata.Depth);

            // Update metadata
            if (confirm && wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Push complete.");
            }
            if (!wasEdited) Logger.Message("Nothing to push.");
        }

        private static bool Push(string directory, FolderMetadata folderMetadata, bool confirm, int maxDepth, int depth = 0)
        {
            var wasEdited = false;

            // Handle files
            foreach (string filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (folderMetadata.Ignore.Contains(fileName))
                {
                    Logger.Info($"Skipping local file '{fileName}'.");
                    continue;
                }
                if (fileName == Defaults.metadataFileName) continue;
                if (folderMetadata.Mappings.TryGetValue(fileName, out var fileMetadata))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime <= fileMetadata.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Edit remote file '{fileName}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.UpdateFile(fileMetadata.FileId, filePath);
                        folderMetadata.Mappings[fileName].Timestamp = DateTime.Now;
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
                else
                {
                    // The file was created
                    wasEdited = true;
                    var message = $"Create remote file '{fileName}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.CreateFile(folderMetadata.FolderId, filePath);
                        folderMetadata.Mappings[fileName] = new()
                        {
                            FileId = file.Id,
                            Timestamp = DateTime.Now
                        };
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }
            foreach (var pair in folderMetadata.Mappings)
            {
                if (folderMetadata.Ignore.Contains(pair.Key))
                {
                    Logger.Info($"Skipping cached remote file '{pair.Key}'.");
                    continue;
                }
                var filePath = Path.Join(directory, pair.Key);
                if (!File.Exists(filePath))
                {
                    // The file was deleted
                    wasEdited = true;
                    var message = $"Delete remote file '{pair.Key}'.";
                    if (confirm)
                    {
                        DriveServiceWrapper.Instance.MoveItemToTrash(pair.Value.FileId);
                        folderMetadata.Mappings.Remove(pair.Key);
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }

            // Handle folders
            if (depth < maxDepth)
            {
                foreach (string folderPath in Directory.GetDirectories(directory))
                {
                    var folderName = Path.GetDirectoryName(folderPath);
                    if (folderMetadata.Ignore.Contains(folderName))
                    {
                        Logger.Info($"Skipping local folder '{folderName}'.");
                        continue;
                    }
                    if (!folderMetadata.Nests.TryGetValue(folderName, out var nestedMetadata))
                    {
                        nestedMetadata = new();

                        // The folder was created
                        wasEdited = true;
                        var message = $"Create remote folder '{folderName}'.";
                        if (confirm)
                        {
                            var folder = DriveServiceWrapper.Instance.CreateFolder(folderMetadata.FolderId, folderName);
                            nestedMetadata.FolderId = folder.Id;
                            Logger.Info(message);
                        }
                        else Logger.ToDo(message);
                        folderMetadata.Nests[folderName] = nestedMetadata;
                    }
                    wasEdited = Push(Path.Join(directory, folderName), nestedMetadata, confirm, maxDepth, depth + 1) || wasEdited;
                }
                foreach (var pair in folderMetadata.Nests)
                {
                    if (folderMetadata.Ignore.Contains(pair.Key))
                    {
                        Logger.Info($"Skipping cached remote folder '{pair.Key}'.");
                        continue;
                    }
                    var folderPath = Path.Join(directory, pair.Key);
                    if (Directory.Exists(folderPath))
                    {
                        wasEdited = Push(Path.Join(directory, pair.Key), pair.Value, confirm, maxDepth, depth + 1) || wasEdited;
                    }
                    else
                    {
                        // The folder was deleted
                        wasEdited = true;
                        var message = $"Delete remote folder '{pair.Key}'.";
                        if (confirm)
                        {
                            DriveServiceWrapper.Instance.MoveItemToTrash(pair.Value.FolderId);
                            folderMetadata.Mappings.Remove(pair.Key);
                            Logger.Info(message);
                        }
                        else Logger.ToDo(message);
                    }
                }
            }
            return wasEdited;
        }
    }
}
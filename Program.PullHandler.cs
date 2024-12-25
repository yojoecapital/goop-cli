using System;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void PullHandler(string workingDirectory, bool verbose, bool confirm)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            var wasEdited = Pull(workingDirectory, metadata.Structure, confirm, metadata.Depth);

            // Update metadata
            if (confirm && wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Pull complete");
            }
            if (!wasEdited) Logger.Message("Nothing to pull.");
        }

        private static bool Pull(string directory, FolderMetadata folderMetadata, bool confirm, int maxDepth, int depth = 0)
        {
            var wasEdited = false;

            // Handle files
            foreach (var pair in folderMetadata.Mappings)
            {
                if (folderMetadata.Ignore.Contains(pair.Key))
                {
                    Logger.Info($"Skipping cached remote file '{pair.Key}'.");
                    continue;
                }
                var filePath = Path.Join(directory, pair.Key);
                if (File.Exists(filePath))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime >= pair.Value.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Update local file '{pair.Key}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetItem(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(directory, file);
                        folderMetadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
                else
                {
                    // The file was created
                    wasEdited = true;
                    var message = $"Create local file '{pair.Key}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetItem(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(directory, file);
                        folderMetadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }
            foreach (string filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (folderMetadata.Ignore.Contains(fileName))
                {
                    Logger.Info($"Skipping local file '{fileName}'.");
                    continue;
                }
                if (
                    fileName == Defaults.metadataFileName ||
                    folderMetadata.Mappings.ContainsKey(fileName)
                ) continue;

                // The file was deleted
                wasEdited = true;
                var message = $"Delete local file '{fileName}'.";
                if (confirm)
                {
                    File.Delete(filePath);
                    Logger.Info(message);
                }
                else Logger.ToDo(message);
            }

            // Handle folders
            if (depth < maxDepth)
            {
                foreach (var pair in folderMetadata.Nests)
                {
                    if (folderMetadata.Ignore.Contains(pair.Key))
                    {
                        Logger.Info($"Skipping cached remote folder '{pair.Key}'.");
                        continue;
                    }

                    // Make sure the directory exists
                    Directory.CreateDirectory(pair.Key);
                    wasEdited = Pull(Path.Join(directory, pair.Key), pair.Value, confirm, maxDepth, depth + 1) || wasEdited;
                }
                foreach (string folderPath in Directory.GetDirectories(directory))
                {
                    var folderName = Path.GetFileName(folderPath);
                    if (folderMetadata.Ignore.Contains(folderName))
                    {
                        Logger.Info($"Skipping local folder '{folderName}'.");
                        continue;
                    }
                    if (folderMetadata.Nests.ContainsKey(folderName)) continue;

                    // The folder was deleted
                    wasEdited = true;
                    var message = $"Delete local folder '{folderName}'.";
                    if (confirm)
                    {
                        Directory.Delete(folderName, true);
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }
            return wasEdited;
        }
    }
}
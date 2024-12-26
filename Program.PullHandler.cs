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
            var wasEdited = Pull(workingDirectory, workingDirectory, metadata.Structure, confirm, metadata.Depth, metadata.Total(workingDirectory));

            // Update metadata
            if (confirm && wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Pull complete");
            }
            if (!wasEdited) Logger.Message("Nothing to pull.");
        }

        private static bool Pull(string directory, string workingDirectory, FolderMetadata folderMetadata, bool confirm, int maxDepth, int total, int depth = 0, int current = 0)
        {
            var wasEdited = false;
            Logger.Info($"Checking to pull into local folder '{directory}'.", depth);
            var relativePath = Path.GetRelativePath(workingDirectory, directory);

            // Handle files
            foreach (var pair in folderMetadata.Mappings)
            {
                if (folderMetadata.Ignore.Contains(pair.Key))
                {
                    Logger.Info($"Skipping cached remote file '{Path.Join(relativePath, pair.Key)}'.", depth);
                    continue;
                }
                var filePath = Path.Join(directory, pair.Key);
                if (File.Exists(filePath))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime <= pair.Value.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Update local file '{Path.Join(relativePath, pair.Key)}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetItem(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(directory, file, depth);
                        folderMetadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        Logger.Info(message, depth);
                    }
                    else Logger.ToDo(message, depth);
                }
                else
                {
                    // The file was created
                    wasEdited = true;
                    var message = $"Create local file '{Path.Join(relativePath, pair.Key)}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetItem(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(directory, file, depth);
                        folderMetadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        Logger.Info(message, depth);
                    }
                    else Logger.ToDo(message, depth);
                }
                Logger.Percent(current++, total);
            }
            foreach (string filePath in Directory.GetFiles(directory))
            {
                var fileName = Path.GetFileName(filePath);
                if (folderMetadata.Ignore.Contains(fileName))
                {
                    Logger.Info($"Skipping local file '{Path.Join(relativePath, fileName)}'.", depth);
                    continue;
                }
                if (
                    fileName == Defaults.metadataFileName ||
                    folderMetadata.Mappings.ContainsKey(fileName)
                ) continue;

                // The file was deleted
                wasEdited = true;
                var message = $"Delete local file '{Path.Join(relativePath, fileName)}'.";
                if (confirm)
                {
                    File.Delete(filePath);
                    Logger.Info(message, depth);
                }
                else Logger.ToDo(message, depth);
                Logger.Percent(current++, total);
            }

            // Handle folders
            if (depth < maxDepth)
            {
                foreach (var pair in folderMetadata.Nests)
                {
                    if (folderMetadata.Ignore.Contains(pair.Key))
                    {
                        Logger.Info($"Skipping cached remote folder '{Path.Join(relativePath, pair.Key)}'.", depth);
                        continue;
                    }

                    // Make sure the directory exists
                    Directory.CreateDirectory(Path.Join(directory, pair.Key));
                    wasEdited = Pull(Path.Join(directory, pair.Key), workingDirectory, pair.Value, confirm, maxDepth, total, depth + 1, current) || wasEdited;
                }
                foreach (string folderPath in Directory.GetDirectories(directory))
                {
                    var folderName = Path.GetFileName(folderPath);
                    if (folderMetadata.Ignore.Contains(folderName))
                    {
                        Logger.Info($"Skipping local folder '{Path.Join(relativePath, folderName)}'.", depth);
                        continue;
                    }
                    if (folderMetadata.Nests.ContainsKey(folderName)) continue;

                    // The folder was deleted
                    wasEdited = true;
                    var message = $"Delete local folder '{Path.Join(relativePath, folderName)}'.";
                    if (confirm)
                    {
                        Directory.Delete(folderPath, true);
                        Logger.Info(message, depth);
                    }
                    else Logger.ToDo(message, depth);
                }
            }
            return wasEdited;
        }
    }
}
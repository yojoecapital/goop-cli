using System;
using System.IO;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void PushHandler(string workingDirectory, bool verbose, bool confirm)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory);
            var wasEdited = false;
            foreach (string filePath in Directory.GetFiles(workingDirectory))
            {
                var fileName = Path.GetFileName(filePath);

                // Skip the metadata file
                if (fileName == Defaults.metadataFileName) continue;
                if (metadata.Mappings.TryGetValue(fileName, out var fileMetadata))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime <= fileMetadata.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Edit remote file '{fileName}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.UpdateFile(fileMetadata.FileId, filePath);
                        metadata.Mappings[fileName].Timestamp = DateTime.Now;
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
                        var file = DriveServiceWrapper.Instance.CreateFile(metadata.FolderId, filePath);
                        metadata.Mappings[fileName] = new()
                        {
                            FileId = file.Id,
                            Timestamp = DateTime.Now
                        };
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }
            foreach (var pair in metadata.Mappings)
            {
                var filePath = Path.Join(workingDirectory, pair.Key);
                if (!File.Exists(filePath))
                {
                    // The file was deleted
                    wasEdited = true;
                    var message = $"Delete remote file '{pair.Key}'.";
                    if (confirm)
                    {
                        DriveServiceWrapper.Instance.MoveFileToTrash(pair.Value.FileId);
                        metadata.Mappings.Remove(pair.Key);
                        Logger.Info(message);
                    }
                    else Logger.ToDo(message);
                }
            }

            // Update metadata
            if (confirm && wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Push complete.");
            }
            if (!wasEdited) Logger.Message("Nothing to push.");
        }
    }
}
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

            // Handle creates and updates
            foreach (string filePath in Directory.GetFiles(workingDirectory))
            {
                var fileName = Path.GetFileName(filePath);

                // Skip the metadata file
                if (fileName == metadataFileName) continue;
                var lastWriteTime = File.GetLastWriteTime(filePath);
                if (metadata.Mappings.TryGetValue(fileName, out var fileMetadata))
                {
                    if (fileMetadata.Timestamp >= lastWriteTime) continue;

                    // The file was modified
                    wasEdited = true;
                    var message = $"Edit remote file '{fileName}'.";
                    if (confirm)
                    {
                        serviceWrapper.UpdateFile(fileMetadata.FileId, filePath);
                        metadata.Mappings[fileName].Timestamp = lastWriteTime;
                        WriteInfo(message);
                    }
                    else WriteToDo(message);
                }
                else
                {
                    // The file doesn't exist
                    wasEdited = true;
                    var message = $"Create remote file '{fileName}'.";
                    if (confirm)
                    {
                        var fileId = serviceWrapper.CreateFile(metadata.FolderId, filePath);
                        metadata.Mappings[fileName] = new ()
                        {
                            FileId = fileId,
                            Timestamp = File.GetLastWriteTime(filePath)
                        };
                        WriteInfo(message);
                    }
                    else WriteToDo(message);
                }    
            }

            // Update metadata
            if (confirm && wasEdited) WriteMetadata(metadata, workingDirectory);

            // Handle deletes
            foreach (var file in serviceWrapper.GetFiles(metadata.FolderId))
            {
                var filePath = Path.Join(workingDirectory, file.Name);
                if (!File.Exists(filePath))
                {
                    // The file was deleted
                    wasEdited = true;
                    var message = $"Delete remote file '{file.Name}'";
                    if (confirm)
                    {
                        serviceWrapper.MoveFileToTrash(file.Id);
                        WriteInfo(message);
                    }
                    else WriteToDo(message);
                }
            }
            if (wasEdited) Console.WriteLine("Push complete.");
            else Console.WriteLine("Nothing to push.");
        }
    }
}
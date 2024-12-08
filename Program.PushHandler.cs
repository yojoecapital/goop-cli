using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

internal partial class Program
{
    private static void PushHandler(string workingDirectory, bool verbose, bool confirm)
    {
        InitializeProgram(verbose);
        var metadata = ReadMetadata(workingDirectory);
        PushLocalFiles(metadata, workingDirectory, confirm);
        RemoveRemoteFiles(metadata, workingDirectory, confirm);

    }

    private static void PushLocalFiles(Metadata metadata, string workingDirectory, bool confirm)
    {
        var filePaths = Directory.GetFiles(workingDirectory);
        var wasEdited = false;
        foreach (string filePath in filePaths)
        {
            var fileName = Path.GetFileName(filePath);

            // Skip the metadata file
            if (fileName == metadataFileName) continue;
            var lastWriteTime = File.GetLastWriteTime(filePath);
            if (metadata.mappings.TryGetValue(fileName, out var fileMetadata) && fileMetadata.timestamp < lastWriteTime)
            {
                // The file was modified
                var message = $"Edit remote file '{fileName}'.";
                if (confirm)
                {
                    UpdateFileInDrive(fileMetadata.fileId, filePath);
                    metadata.mappings[fileName].timestamp = lastWriteTime;
                    WriteInfo(message);
                    wasEdited = true;
                }
                else WriteToDo(message);
            }
            else
            {
                // The file doesn't exist
                var message = $"Create remote file '{fileName}'.";
                if (confirm)
                {
                    var fileId = CreateFileInDrive(metadata.folderId, filePath);
                    metadata.mappings[fileName] = new ()
                    {
                        fileId = fileId,
                        timestamp = File.GetLastWriteTime(filePath)
                    };
                    WriteInfo(message);
                    wasEdited = true;
                }
                else WriteToDo(message);
            }    
        }
        if (wasEdited) WriteMetadata(metadata, workingDirectory);
    }

    private static void RemoveRemoteFiles(Metadata metadata, string workingDirectory, bool confirm)
    {
        foreach (var file in GetFilesInDrive(metadata.folderId))
        {
            var filePath = Path.Join(workingDirectory, file.Name);
            if (!File.Exists(filePath))
            {
                // The file was deleted
                var message = $"Delete remote file '{file.Name}'";
                if (confirm)
                {
                    MoveFileToTrashInDrive(file.Id);
                    WriteInfo(message);
                }
                else WriteToDo(message);
            }
        }
    }
}
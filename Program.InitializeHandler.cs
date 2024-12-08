using System;
using System.Collections.Generic;
using System.IO;

internal partial class Program
{
    private static readonly string metadataFileName = ".goop";

    private static void InitializeHandler(string workingDirectory, bool verbose, string folderId)
    {
        InitializeProgram(verbose);

        // Ensure working directory exists and is empty
        Directory.CreateDirectory(workingDirectory);
        if (Directory.GetFiles(workingDirectory).Length > 0) throw new InvalidOperationException($"The working directory '{workingDirectory}' is not empty.");

        // Ensure folder exists in Google Drive
        var folderRequest = service.Files.Get(folderId);
        folderRequest.Fields = "id, name, mimeType";
        Google.Apis.Drive.v3.Data.File folder;
        try
        {
            folder = folderRequest.Execute();
        }
        catch
        {
            throw new InvalidOperationException($"Folder with ID '{folderId}' does not exist in Google Drive.");
        }

        // Ensure the retrieved item is a file
        if (folder.MimeType != "application/vnd.google-apps.folder") throw new InvalidOperationException($"The provided ID '{folderId}' is not a folder.");
        if (verbose) WriteInfo($"Initializing sync for folder '{folder.Name}'...");

        // Traverse Google Drive folder, copy files, and create metadata file 
        var metadata = new Metadata
        {
            folderId = folderId,
            mappings = CopyDriveFolderToLocal(folderId, workingDirectory)
        };

        // Write metadata
        WriteMetadata(metadata, workingDirectory);
        Console.WriteLine("Initialization complete.");
    }

    private static Dictionary<string, FileMetadata> CopyDriveFolderToLocal(string folderId, string workingDirectory)
    {
        var mappings = new Dictionary<string, FileMetadata>();
        foreach (var file in GetFilesInDrive(folderId))
        {
            DownloadFileFromDrive(workingDirectory, file);

            // Add metadata
            mappings[file.Name] = new()
            {
                timestamp = File.GetLastWriteTime(file.Name),
                fileId = file.Id
            };
        }
        return mappings;
    }
}
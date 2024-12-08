using System;
using System.Collections.Generic;
using System.IO;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void InitializeHandler(string workingDirectory, bool verbose, string folderId)
        {
            InitializeProgram(verbose);

            // Ensure working directory exists and is empty
            Directory.CreateDirectory(workingDirectory);
            if (Directory.GetFiles(workingDirectory).Length > 0) throw new InvalidOperationException($"The working directory '{workingDirectory}' is not empty.");

            var folderName = serviceWrapper.GetFolderName(folderId);
            WriteInfo($"Initializing sync for folder '{folderName}'.");

            // Traverse Google Drive folder, copy files, and create metadata file 
            var metadata = new Metadata
            {
                FolderId = folderId,
                Mappings = CopyDriveFolderToLocal(folderId, workingDirectory)
            };

            // Write metadata
            WriteMetadata(metadata, workingDirectory);
            Console.WriteLine("Initialization complete.");
        }

        private static Dictionary<string, FileMetadata> CopyDriveFolderToLocal(string folderId, string workingDirectory)
        {
            var mappings = new Dictionary<string, FileMetadata>();
            foreach (var file in serviceWrapper.GetFiles(folderId))
            {
                var filePath = serviceWrapper.DownloadFile(workingDirectory, file);

                // Add metadata
                mappings[file.Name] = new()
                {
                    Timestamp = File.GetLastWriteTime(filePath),
                    FileId = file.Id
                };
            }
            return mappings;
        }
    }
}
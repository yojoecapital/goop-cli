using System;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void InitializeHandler(string workingDirectory, bool verbose, string folderId, int depth)
        {
            InitializeProgram(verbose);
            var rootDirectory = GetRootFolder(workingDirectory);
            if (rootDirectory != null) throw new Exception($"An existing metadata file at {rootDirectory} was found. Remove the '{Defaults.metadataFileName}' file or initialize elsewhere.");
            if (depth < 0) throw new Exception("Depth must be at least 0");
            Logger.Message("Creating metadata file...");

            // Ensure working directory exists and is empty
            Directory.CreateDirectory(workingDirectory);

            // Create metadata
            var metadata = new Metadata()
            {
                Structure = CreateFolderMetadata(folderId, depth),
                Depth = depth
            };

            // Write metadata
            WriteMetadata(metadata, workingDirectory);
            Logger.Message("Initialization complete. Ready to pull.");
        }

        private static FolderMetadata CreateFolderMetadata(string folderId, int maxDepth, int depth = 0)
        {
            var items = DriveServiceWrapper.Instance.GetItems(folderId, out var folder);
            Logger.Info($"{string.Concat(Enumerable.Repeat("+ ", depth))}Initializing sync for folder '{folder.Name}' ({folder.Id}).");
            var parent = DriveServiceWrapper.Instance.GetItem(folderId);

            // Handle mappings
            var files = items.Where(item => item.MimeType != DriveServiceWrapper.folderMimeType);
            var mappings = files.ToDictionary(
                file => file.Name,
                file => new FileMetadata()
                {
                    Timestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime,
                    FileId = file.Id
                }
            );

            // Handles nests
            var folders = items.Where(item => item.MimeType == DriveServiceWrapper.folderMimeType);
            var nests = depth < maxDepth ? folders.ToDictionary(
                folder => folder.Name,
                folder => CreateFolderMetadata(folder.Id, maxDepth, depth + 1)
            ) : [];

            // Return the metadata
            return new FolderMetadata()
            {
                FolderId = folderId,
                Mappings = mappings,
                Nests = nests
            };
        }
    }
}
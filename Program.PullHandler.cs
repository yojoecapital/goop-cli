using System;
using System.IO;
using System.Linq;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void PullHandler(string workingDirectory, bool verbose, bool confirm)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory);
            var wasEdited = false;
            foreach (var pair in metadata.Mappings)
            {
                var filePath = Path.Join(workingDirectory, pair.Key);
                if (File.Exists(filePath) && metadata.Mappings.TryGetValue(pair.Key, out var fileMetadata))
                {
                    var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
                    if (lastWriteTime >= pair.Value.Timestamp) continue;

                    // The file was updated
                    wasEdited = true;
                    var message = $"Update local file '{pair.Key}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetFile(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(workingDirectory, file);
                        metadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        WriteInfo(message);
                    }
                    else WriteToDo(message);
                }
                else
                {
                    // The file was created
                    wasEdited = true;
                    var message = $"Create local file '{pair.Key}'.";
                    if (confirm)
                    {
                        var file = DriveServiceWrapper.Instance.GetFile(pair.Value.FileId);
                        DriveServiceWrapper.Instance.DownloadFile(workingDirectory, file);
                        metadata.Mappings[pair.Key] = new()
                        {
                            Timestamp = File.GetLastWriteTimeUtc(filePath),
                            FileId = file.Id
                        };
                        WriteInfo(message);
                    }
                    else WriteToDo(message);
                }
            }
            foreach (string filePath in Directory.GetFiles(workingDirectory))
            {
                var fileName = Path.GetFileName(filePath);
                if (fileName == Defaults.metadataFileName || metadata.Mappings.ContainsKey(fileName)) continue;

                // The file was deleted
                wasEdited = true;
                var message = $"Delete local file '{fileName}'.";
                if (confirm)
                {
                    File.Delete(filePath);
                    WriteInfo(message);
                }
                else WriteToDo(message);
            }

            // Update metadata
            if (confirm && wasEdited) 
            {
                WriteMetadata(metadata, workingDirectory);
                Console.WriteLine("Pull complete");
            }
            if (!wasEdited) Console.WriteLine("Nothing to pull.");
        }
    }
}
using System;
using System.Linq;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void FetchHandler(string workingDirectory, bool verbose)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory);
            var wasEdited = false;
            var remoteFiles = DriveServiceWrapper.Instance.GetFiles(metadata.FolderId).ToDictionary(file => file.Id);
            foreach (var pair in metadata.Mappings)
            {
                WriteInfo($"Checking if remote file '{pair.Key}' ({pair.Value.FileId}) was deleted.");
                if (!remoteFiles.ContainsKey(pair.Value.FileId))
                {
                    wasEdited = true;
                    metadata.Mappings.Remove(pair.Key);
                    WriteInfo($"Remote file {pair.Key} ({pair.Value.FileId}) was deleted.");
                }
            }
            foreach (var file in remoteFiles.Values)
            {
                if (metadata.Mappings.TryGetValue(file.Name, out var fileMetadata))
                {
                    var timestamp = fileMetadata.Timestamp;
                    var remoteTimestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime;
                    WriteInfo($"Checking if remote file '{file.Name}' ({file.Id}) was updated by comparing remote time '{remoteTimestamp}' to cached remote time '{timestamp}' (>).");
                    if (remoteTimestamp > timestamp)
                    {
                        wasEdited = true;
                        fileMetadata.Timestamp = remoteTimestamp;
                        WriteInfo($"Remote file {file.Name} ({file.Id}) was updated from {timestamp} to {remoteTimestamp}.");
                    }
                }
                else
                {
                    wasEdited = true;
                    metadata.Mappings[file.Name] = new()
                    {
                        Timestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime,
                        FileId = file.Id
                    };
                    WriteInfo($"Remote file {file.Name} ({file.Id}) was created.");
                }
            }

            // Update metadata
            if (wasEdited) 
            {
                WriteMetadata(metadata, workingDirectory);
                Console.WriteLine("Fetch complete.");
            }
            else Console.WriteLine("Up to date.");
        }
    }
}
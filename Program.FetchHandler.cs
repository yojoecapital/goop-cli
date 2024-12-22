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
                Logger.Info($"Checking if remote file '{pair.Key}' was deleted.");
                if (!remoteFiles.ContainsKey(pair.Value.FileId))
                {
                    wasEdited = true;
                    metadata.Mappings.Remove(pair.Key);
                    Logger.Info($"Remote file {pair.Key} was deleted.");
                }
            }
            foreach (var file in remoteFiles.Values)
            {
                if (metadata.Mappings.TryGetValue(file.Name, out var fileMetadata))
                {
                    var timestamp = fileMetadata.Timestamp;
                    var remoteTimestamp = file.ModifiedTimeDateTimeOffset.Value.DateTime;
                    Logger.Info($"Checking if remote file '{file.Name}' was updated by comparing remote time '{remoteTimestamp}' to cached remote time '{timestamp}' (>).");
                    if (remoteTimestamp > timestamp)
                    {
                        wasEdited = true;
                        fileMetadata.Timestamp = remoteTimestamp;
                        Logger.Info($"Remote file {file.Name} was updated from {timestamp} to {remoteTimestamp}.");
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
                    Logger.Info($"Remote file {file.Name} was created.");
                }
            }

            // Update metadata
            if (wasEdited)
            {
                WriteMetadata(metadata, workingDirectory);
                Logger.Message("Fetch complete.");
            }
            else Logger.Message("Up to date.");
        }
    }
}
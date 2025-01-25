using System;
using GoogleDrivePushCli.Data;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands
{
    public static class DepthHandler
    {
        public static void Handle(string workingDirectory, int? depth)
        {
            Metadata metadata;
            if (!depth.HasValue)
            {
                metadata = MetadataHelpers.ReadMetadata(workingDirectory, out _);
                Logger.Message(metadata.Depth.ToString());
                return;
            }
            if (depth < 0) throw new Exception("Depth must be at least 0");
            metadata = MetadataHelpers.ReadMetadata(workingDirectory, out workingDirectory);
            if (metadata.Depth == depth) Logger.Info($"Depth is already set to {metadata.Depth}.");
            else
            {
                metadata.Depth = depth.Value;
                MetadataHelpers.WriteMetadata(metadata, workingDirectory);
            }
        }
    }
}
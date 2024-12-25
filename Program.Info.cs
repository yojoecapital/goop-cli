using System;
using System.IO;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void DepthHandler(string workingDirectory, bool verbose, int maxDepth)
        {
            InitializeProgram(verbose);
            if (maxDepth <= 0) throw new Exception("Depth must be greater than 0");
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            if (metadata.Depth == maxDepth)
            {
                Logger.Info($"Depth is already set to {metadata.Depth}.");
                return;
            }
            metadata.Depth = maxDepth;
            WriteMetadata(metadata, workingDirectory);
            Logger.Message($"Depth is {maxDepth}.");
        }

        private static void InfoHandler(string workingDirectory, bool verbose)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory, out _);
            Logger.Message($"Depth: {metadata.Depth}");
            Logger.Message($"Cached remote files: {metadata.Count()}");
            Logger.Message($"Local files: {metadata.Count()}");
            Logger.Message("Ignoring:");
            LogIgnoredFiles(metadata.Structure, workingDirectory, workingDirectory);
        }

        private static void LogIgnoredFiles(FolderMetadata folderMetadata, string workingDirectory, string directory)
        {
            foreach (var name in folderMetadata.Ignore)
            {
                Logger.Message(Path.GetRelativePath(workingDirectory, Path.Join(directory, name)));
            }
            foreach (var pair in folderMetadata.Nests)
            {
                LogIgnoredFiles(pair.Value, workingDirectory, Path.Join(directory, pair.Key));
            }
        }
    }
}
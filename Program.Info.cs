using System;
using System.Collections.Generic;
using System.IO;
using GoogleDrivePushCli.Meta;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void DepthHandler(string workingDirectory, bool verbose, int? depth)
        {
            InitializeProgram(verbose);
            Metadata metadata;
            if (!depth.HasValue)
            {
                metadata = ReadMetadata(workingDirectory, out _);
                Logger.Message(metadata.Depth.ToString());
                return;
            }
            if (depth < 0) throw new Exception("Depth must be at least 0");
            metadata = ReadMetadata(workingDirectory, out workingDirectory);
            if (metadata.Depth == depth) Logger.Info($"Depth is already set to {metadata.Depth}.");
            else
            {
                metadata.Depth = depth.Value;
                WriteMetadata(metadata, workingDirectory);
            }
        }

        private static void InfoHandler(string workingDirectory, bool verbose)
        {
            InitializeProgram(verbose);
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            Logger.Message($"Depth: {metadata.Depth}");
            Logger.Message($"Cached remote files: {metadata.Count()}");
            Logger.Message($"Local files: {metadata.Count(workingDirectory)}");
            var items = new List<string>();
            GrabIgnoredFiles(metadata.Structure, workingDirectory, workingDirectory, items);
            Logger.Message($"Ignoring: {items.Count}");
            foreach (var item in items) Logger.Message(item, 1);
        }

        private static void GrabIgnoredFiles(FolderMetadata folderMetadata, string workingDirectory, string directory, List<string> items)
        {
            foreach (var name in folderMetadata.Ignore)
            {
                items.Add(Path.GetRelativePath(workingDirectory, Path.Join(directory, name)));
            }
            foreach (var pair in folderMetadata.Nests)
            {
                GrabIgnoredFiles(pair.Value, workingDirectory, Path.Join(directory, pair.Key), items);
            }
        }
    }
}
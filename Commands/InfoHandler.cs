using System.Collections.Generic;
using System.IO;
using GoogleDrivePushCli.Data;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands
{
    public static class InfoHandler
    {
        public static void Handle(string workingDirectory)
        {
            var metadata = MetadataHelpers.ReadMetadata(workingDirectory, out workingDirectory);
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
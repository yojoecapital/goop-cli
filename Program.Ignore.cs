using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void IgnoreHandler(string workingDirectory, bool verbose, string ingorePath, bool add, bool remove)
        {
            InitializeProgram(verbose);
            var name = Path.GetFileName(ingorePath);
            var path = Path.GetDirectoryName(ingorePath);
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            var folderMetadata = metadata.Structure;
            var trackingPath = workingDirectory;
            if (!string.IsNullOrEmpty(path))
            {
                foreach (var key in SplitPathIntoParts(path))
                {
                    if (key == ".") continue;
                    trackingPath = Path.Join(trackingPath, key);
                    if (folderMetadata.Nests.TryGetValue(key, out var nestedMetadata))
                    {
                        folderMetadata = nestedMetadata;
                    }
                    else
                    {
                        throw new Exception($"The path at '{Path.GetRelativePath(workingDirectory, trackingPath)}' is not being tracked.");
                    }
                }
            }
            if (remove)
            {
                if (!folderMetadata.Ignore.Remove(name)) Logger.Info($"The item '{name}' was not being ignored.");
                else WriteMetadata(metadata, workingDirectory);
                Logger.Message($"Not ignoring item '{name}' on the path '{Path.GetRelativePath(workingDirectory, trackingPath)}'.");
            }
            else if (add)
            {
                if (!folderMetadata.Ignore.Add(name)) Logger.Info($"The item '{name}' was already being ignored.");
                else WriteMetadata(metadata, workingDirectory);
                Logger.Message($"Ignoring item '{name}' on the path '{Path.GetRelativePath(workingDirectory, trackingPath)}'.");
            }
        }

        private static IEnumerable<string> SplitPathIntoParts(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be null or empty.", nameof(path));

            while (!string.IsNullOrEmpty(path))
            {
                string part = Path.GetFileName(path);
                if (string.IsNullOrEmpty(part))
                {
                    yield return path;
                    yield break;
                }

                yield return part;
                path = Path.GetDirectoryName(path);
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.IO;

namespace GoogleDrivePushCli
{
    internal partial class Program
    {
        private static void IgnoreHandler(string workingDirectory, bool verbose, string ingorePath)
        {
            InitializeProgram(verbose);
            var name = Path.GetFileName(ingorePath);
            var path = Path.GetDirectoryName(ingorePath);
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            var folderMetadata = metadata.Structure;
            var trackingPath = workingDirectory;
            foreach (var key in SplitPathIntoParts(path))
            {
                trackingPath = Path.Join(trackingPath, key);
                if (folderMetadata.Nests.TryGetValue(key, out var nestedMetadata))
                {
                    folderMetadata = nestedMetadata;
                }
                else
                {
                    throw new Exception($"The path at '${Path.GetRelativePath(workingDirectory, trackingPath)}' is not being tracked.");
                }
            }
            folderMetadata.Ignore.Add(name);
            WriteMetadata(metadata, workingDirectory);
            Logger.Message($"Ignoring item '{name}' on the path ${trackingPath}");
        }

        private static void TrackHandler(string workingDirectory, bool verbose, string ingorePath)
        {
            InitializeProgram(verbose);
            var name = Path.GetFileName(ingorePath);
            var path = Path.GetDirectoryName(ingorePath);
            var metadata = ReadMetadata(workingDirectory, out workingDirectory);
            var folderMetadata = metadata.Structure;
            var trackingPath = workingDirectory;
            foreach (var key in SplitPathIntoParts(path))
            {
                trackingPath = Path.Join(trackingPath, key);
                if (folderMetadata.Nests.TryGetValue(key, out var nestedMetadata))
                {
                    folderMetadata = nestedMetadata;
                }
                else
                {
                    throw new Exception($"The path at '${Path.GetRelativePath(workingDirectory, trackingPath)}' is not being tracked.");
                }
            }
            if (!folderMetadata.Ignore.Remove(name)) Logger.Info($"The item '{name}' was not being ignored");
            else WriteMetadata(metadata, workingDirectory);
            Logger.Message($"Tracking item '{name}' on the path ${trackingPath}");
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
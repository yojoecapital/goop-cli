using System;
using System.IO;

namespace GoogleDrivePushCli.Utilities;

public static class FileManagementHelpers
{
    public static int CountFilesAtDepth(string directoryPath, int depth, int currentDepth)
    {
        if (currentDepth > depth) return 0;
        var total = 0;
        var files = Directory.GetFiles(directoryPath);
        total += files.Length;
        if (currentDepth < depth)
        {
            var subdirectories = Directory.GetDirectories(directoryPath);
            foreach (string subdirectory in subdirectories)
            {
                total += CountFilesAtDepth(subdirectory, depth, currentDepth + 1);
            }
        }
        return total;
    }
}
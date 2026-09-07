using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GoogleDrivePushCli.Utilities;

public static class FileManagementHelpers
{
    private static readonly UnixFileMode executableBits =
        UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

    public static int CountFilesAtDepth(string directoryPath, int depth, int currentDepth)
    {
        if (currentDepth >= depth) return 0;
        var total = Directory.GetFiles(directoryPath).Length;
        foreach (string subdirectory in Directory.GetDirectories(directoryPath))
        {
            total += CountFilesAtDepth(subdirectory, depth, currentDepth + 1);
        }
        return total;
    }

    public static void CopyFileWithPermissions(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, true);
        CopyUnixFileMode(sourcePath, destinationPath);
    }

    public static void CopyUnixFileMode(string sourcePath, string destinationPath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try
        {
            File.SetUnixFileMode(destinationPath, File.GetUnixFileMode(sourcePath));
        }
        catch (Exception exception)
        {
            ConsoleHelpers.Info($"Failed to copy permissions to '{destinationPath}': {exception.Message}");
        }
    }

    public static void MakeExecutable(string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
        try
        {
            File.SetUnixFileMode(filePath, File.GetUnixFileMode(filePath) | executableBits);
        }
        catch (Exception exception)
        {
            ConsoleHelpers.Info($"Failed to make '{filePath}' executable: {exception.Message}");
        }
    }
}

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

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

    public static void CopyFileWithPermissions(string sourcePath, string destinationPath)
    {
        File.Copy(sourcePath, destinationPath, true);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var sourceFileInfo = new FileInfo(sourcePath);
            var destFileInfo = new FileInfo(destinationPath);
            FileSecurity fileSecurity = sourceFileInfo.GetAccessControl();
            destFileInfo.SetAccessControl(fileSecurity);
        }
        else
        {
            string escapedSource = EscapeForShell(sourcePath);
            string escapedDest = EscapeForShell(destinationPath);

            string command = $"cp -p {escapedSource} {escapedDest}";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new Exception($"Failed to copy file permissions: {error}");
            }
        }
    }

    private static string EscapeForShell(string path)
    {
        if (string.IsNullOrEmpty(path)) return "''";
        return "'" + path.Replace("'", "'\\''") + "'";
    }
}
using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GoogleDrivePushCli.Services;

public class IgnoreListService
{
    private readonly string directory;
    private readonly Matcher matcher = new();

    public static bool ExistsInDirectory(string directory)
    {
        return File.Exists(Path.Join(directory, Defaults.ignoreListFileName));
    }

    public IgnoreListService(string directory)
    {
        this.directory = directory;
        var filePath = Path.Join(directory, Defaults.ignoreListFileName);
        foreach (var pattern in File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)))
        {
            matcher.AddInclude(pattern);
        }
    }

    public bool ShouldIgnore(string fullPath)
    {
        string relativePath = GetRelativeFilePath(directory, fullPath);
        if (relativePath == null) return true;
        return matcher.Match(relativePath).HasMatches;
    }

    private static string GetRelativeFilePath(string directory, string filePath)
    {
        string fullDirectory = Path.GetFullPath(directory);
        string fullFilePath = Path.GetFullPath(filePath);
        if (!fullFilePath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        string relativePath = Path.GetRelativePath(fullDirectory, fullFilePath);
        return relativePath.StartsWith("./") || relativePath.StartsWith(".\\")
            ? Path.GetFileName(filePath)
            : relativePath;
    }
}

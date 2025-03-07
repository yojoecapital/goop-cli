using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GoogleDrivePushCli.Services;

public class IgnoreListService
{
    private readonly string directory;
    private readonly Matcher matcher = new();

    public IgnoreListService(string directory)
    {
        this.directory = directory;
        var filePath = Path.Join(directory, Defaults.ignoreListFileName);
        if (File.Exists(filePath))
        {
            foreach (var pattern in File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                matcher.AddInclude(pattern);
            }
        }
    }

    public bool ShouldIgnore(string relativePath)
    {
        return matcher.Match(relativePath).HasMatches;
    }
}

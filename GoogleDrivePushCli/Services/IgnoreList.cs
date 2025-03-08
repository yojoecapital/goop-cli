using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Json.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GoogleDrivePushCli.Services;

public class IgnoreList
{
    private readonly Matcher matcher = new();
    private readonly List<string> autoIgnoreList = ApplicationConfiguration.Instance.AutoIgnoreList;

    public IgnoreList(string directory)
    {
        var filePath = Path.Join(directory, Defaults.ignoreListFileName);
        if (File.Exists(filePath))
        {
            foreach (var pattern in File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                matcher.AddInclude(pattern);
            }
        }
        matcher.AddIncludePatterns(autoIgnoreList);
    }

    public void AddAll(string[] patterns)
    {
        matcher.AddIncludePatterns(patterns);
    }

    public bool ShouldIgnore(string relativePath)
    {
        return matcher.Match(relativePath).HasMatches;
    }
}

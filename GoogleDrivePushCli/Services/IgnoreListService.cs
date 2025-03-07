using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleDrivePushCli.Json.Configuration;
using Microsoft.Extensions.FileSystemGlobbing;

namespace GoogleDrivePushCli.Services;

public class IgnoreListService
{
    private readonly Matcher matcher = new();
    private readonly List<string> autoIgnoreList = ApplicationConfiguration.Instance.AutoIgnoreList;

    public IgnoreListService(string directory)
    {
        var filePath = Path.Join(directory, Defaults.ignoreListFileName);
        if (File.Exists(filePath))
        {
            foreach (var pattern in File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                matcher.AddInclude(pattern);
            }
        }
        foreach (var pattern in autoIgnoreList)
        {
            matcher.AddInclude(pattern);
        }
    }

    public bool ShouldIgnore(string relativePath)
    {
        return matcher.Match(relativePath).HasMatches;
    }
}

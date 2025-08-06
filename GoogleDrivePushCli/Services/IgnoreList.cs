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
                AddPattern(pattern);
            }
        }
        AddPatterns(autoIgnoreList);
    }

    public void AddAll(string[] patterns)
    {
        AddPatterns(patterns);
    }

    private void AddPatterns(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            AddPattern(pattern);
        }
    }

    private void AddPattern(string pattern)
    {
        if (pattern.StartsWith('!'))
        {
            // Negation pattern: exclude
            var negatedPattern = pattern.Substring(1);
            matcher.AddExclude(negatedPattern);
        }
        else
        {
            // Normal include pattern
            matcher.AddInclude(pattern);
        }
    }

    public bool ShouldIgnore(string relativePath)
    {
        return matcher.Match(relativePath).HasMatches;
    }
}

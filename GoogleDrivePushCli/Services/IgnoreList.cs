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
            AddPatterns(File.ReadLines(filePath).Select(line => line.Trim()).Where(IsPattern));
        }
        AddPatterns(autoIgnoreList);
    }

    private static bool IsPattern(string line) => !string.IsNullOrWhiteSpace(line) && !line.StartsWith('#');

    public void AddAll(string[] patterns)
    {
        if (patterns == null) return;
        AddPatterns(patterns.Where(IsPattern));
    }

    private void AddPatterns(IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns) AddPattern(pattern);
    }

    private void AddPattern(string pattern)
    {
        if (pattern.StartsWith('!')) matcher.AddExclude(Normalize(pattern[1..]));
        else matcher.AddInclude(Normalize(pattern));
    }

    private static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    public bool ShouldIgnore(string relativePath)
    {
        var normalized = Normalize(relativePath);
        if (normalized.Length == 0) return false;
        return matcher.Match(normalized).HasMatches;
    }
}

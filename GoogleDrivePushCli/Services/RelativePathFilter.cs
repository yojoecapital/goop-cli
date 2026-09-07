using System;
using System.IO;

namespace GoogleDrivePushCli.Services;

public class RelativePathFilter
{
    public static readonly RelativePathFilter AllowAll = new(null, null);

    private readonly IgnoreList ignoreList;
    private readonly string baseDirectory;

    public RelativePathFilter(IgnoreList ignoreList, string baseDirectory)
    {
        this.ignoreList = ignoreList;
        this.baseDirectory = baseDirectory;
    }

    public bool ShouldIgnore(string fullPath)
    {
        if (ignoreList == null || string.IsNullOrEmpty(baseDirectory)) return false;
        var relativePath = Path.GetRelativePath(baseDirectory, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal)) return false;
        return ignoreList.ShouldIgnore(relativePath);
    }
}

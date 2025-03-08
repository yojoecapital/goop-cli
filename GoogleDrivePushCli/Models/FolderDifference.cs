using System;
using Spectre.Console;

namespace GoogleDrivePushCli.Models;

public class FolderDifference
{
    public bool ExistsLocally { get; set; }
    public bool ExistsRemotely { get; set; }

    private string path;
    public string Path
    {
        set
        {
            path = value;
        }
        get
        {
            if (ExistsLocally && !ExistsRemotely)
            {
                return $"[green]{path.EscapeMarkup()}[/]";
            }
            if (!ExistsLocally && ExistsRemotely)
            {
                return $"[red]{path.EscapeMarkup()}[/]";
            }
            return path.EscapeMarkup();
        }
    }
}
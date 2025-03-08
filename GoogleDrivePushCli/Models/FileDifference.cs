using System;
using Spectre.Console;

namespace GoogleDrivePushCli.Models;

public class FileDifference
{
    public DateTime? LocalDateTime { get; set; }
    public DateTime? RemoteDateTime { get; set; }

    public string path;
    public string Path
    {
        set
        {
            path = value;
        }
        get
        {
            if (LocalDateTime.HasValue && !RemoteDateTime.HasValue)
            {
                return $"[green]{path.EscapeMarkup()}[/]";
            }
            if (!LocalDateTime.HasValue && RemoteDateTime.HasValue)
            {
                return $"[red]{path.EscapeMarkup()}[/]";
            }
            if (LocalDateTime.HasValue && RemoteDateTime.HasValue && LocalDateTime.Value != RemoteDateTime.Value)
            {
                return $"[yellow]{path.EscapeMarkup()}[/]";
            }
            return path.EscapeMarkup();
        }
    }
}
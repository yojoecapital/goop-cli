using System;

namespace GoogleDrivePushCli.Models;

public class FileDifference
{
    public DateTime? LocalDateTime { get; set; }
    public DateTime? RemoteDateTime { get; set; }
    public string Path { get; set; }
}
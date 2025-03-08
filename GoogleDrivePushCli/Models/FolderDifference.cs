using System;

namespace GoogleDrivePushCli.Models;

public class FolderDifference
{
    public bool ExistsLocally { get; set; }
    public bool ExistsRemotely { get; set; }
    public string Path { get; set; }
}
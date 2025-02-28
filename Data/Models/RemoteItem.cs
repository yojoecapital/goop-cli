using System;

namespace GoogleDrivePushCli.Data.Models;

public class RemoteItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string MimeType { get; set; }
    public DateTime? ModifiedTime { get; set; }
    public long? Size { get; set; }
    public bool IsFolder => MimeType == DriveServiceWrapper.folderMimeType;

    public override string ToString()
    {
        if (IsFolder) return $"{Name}/";
        return Name;
    }
}
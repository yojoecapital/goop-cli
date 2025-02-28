using System;

namespace GoogleDrivePushCli.Data.Models;

public class RemoteItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string MimeType { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long? Size { get; set; }
    public bool Trashed { get; set; }
}
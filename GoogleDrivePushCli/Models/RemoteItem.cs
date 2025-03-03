namespace GoogleDrivePushCli.Models;

public class RemoteItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FolderId { get; set; }
    public bool Trashed { get; set; }
    public long Timestamp { get; set; }
}
namespace GoogleDrivePushCli.Models;

public class RemoteItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string RemoteName { get; set; }
    public string FolderId { get; set; }
    public long Timestamp { get; set; }

    public string EffectiveRemoteName => string.IsNullOrEmpty(RemoteName) ? Name : RemoteName;

    public override string ToString() => Name;
}

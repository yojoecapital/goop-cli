using System;
using System.Linq;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Models;

public class RemoteFile
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string MimeType { get; set; }
    public DateTime ModifiedTime { get; set; }
    public long Size { get; set; }
    public string FolderId { get; set; }
    public bool Trashed { get; set; }
    public long Timestamp { get; set; }

    public override string ToString() => Name;

    public static RemoteFile CreateFrom(GoogleDriveFile googleDriveFile)
    {
        return new()
        {
            Id = googleDriveFile.Id,
            Name = googleDriveFile.Name,
            MimeType = googleDriveFile.MimeType,
            ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset.Value.DateTime,
            Size = googleDriveFile.Size.Value,
            FolderId = googleDriveFile.Parents.First()
        };
    }
}
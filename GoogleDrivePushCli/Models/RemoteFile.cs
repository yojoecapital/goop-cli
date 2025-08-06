using System.Linq;
using GoogleDrivePushCli.Utilities;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Models;

public class RemoteFile : RemoteItem
{
    public string MimeType { get; set; }
    public long ModifiedTime { get; set; }
    public long Size { get; set; }
    public bool Trashed { get; set; }

    public static RemoteFile CreateFrom(GoogleDriveFile googleDriveFile)
    {
        string name = googleDriveFile.Name;
        if (LinkFileHelper.IsGoogleDriveNativeFile(googleDriveFile.MimeType))
        {
            name += LinkFileHelper.GetLinkFileExtension();
        }
        return new()
        {
            Id = googleDriveFile.Id,
            Name = name,
            MimeType = googleDriveFile.MimeType,
            ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset.Value.ToUnixTimeMilliseconds(),
            Size = googleDriveFile.Size.Value,
            FolderId = googleDriveFile.Parents?.FirstOrDefault()
        };
    }
}
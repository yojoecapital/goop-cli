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

    public bool IsLink => LinkFileHelper.IsGoogleDriveNativeFile(MimeType);

    public string WebViewLink => $"https://drive.google.com/file/d/{Id}/view";

    public static RemoteFile CreateFrom(GoogleDriveFile googleDriveFile)
    {
        var remoteName = googleDriveFile.Name;
        var localName = LinkFileHelper.IsGoogleDriveNativeFile(googleDriveFile.MimeType)
            ? remoteName + LinkFileHelper.GetLinkFileExtension()
            : remoteName;
        return new()
        {
            Id = googleDriveFile.Id,
            Name = localName,
            RemoteName = remoteName,
            MimeType = googleDriveFile.MimeType,
            ModifiedTime = googleDriveFile.ModifiedTimeDateTimeOffset?.ToUnixTimeMilliseconds() ?? 0,
            Size = googleDriveFile.Size ?? 0,
            Trashed = googleDriveFile.Trashed ?? false,
            FolderId = googleDriveFile.Parents?.FirstOrDefault()
        };
    }
}

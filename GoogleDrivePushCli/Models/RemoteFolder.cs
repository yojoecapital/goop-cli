using System.Linq;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Models;

public class RemoteFolder : RemoteItem
{
    public static string MimeType => "application/vnd.google-apps.folder";

    public bool Populated { get; set; }

    public override string ToString() => $"{Name}/";

    public static RemoteFolder CreateFrom(GoogleDriveFile googleDriveFolder)
    {
        return new()
        {
            Id = googleDriveFolder.Id,
            Name = googleDriveFolder.Name,
            FolderId = googleDriveFolder.Parents?.FirstOrDefault()
        };
    }
}
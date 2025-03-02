using System.Collections.Generic;
using System.Linq;
using GoogleDriveFile = Google.Apis.Drive.v3.Data.File;

namespace GoogleDrivePushCli.Models;

public class RemoteFolder
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string FolderId { get; set; }
    public long Timestamp { get; set; }

    public List<RemoteFile> RemoteFiles { get; set; }

    public override string ToString() => Name;

    public static RemoteFolder CreateFrom(GoogleDriveFile googleDriveFile)
    {
        return new()
        {
            Id = googleDriveFile.Id,
            Name = googleDriveFile.Name,
            FolderId = googleDriveFile.Parents.First()
        };
    }
}
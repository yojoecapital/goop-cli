using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;

namespace GoogleDrivePushCli.Commands.Remote;

public class ListCommand : Command
{
    public ListCommand() : base("list", "List the items in a remote folder.")
    {
        AddAlias("ls");
        var pathArgument = new Argument<string>("path", "The path of the remote folder.");
        pathArgument.SetDefaultValue("/");
        AddArgument(pathArgument);
        this.SetHandler(
            Handle,
            pathArgument
        );
    }

    private static void Handle(string path)
    {
        var remoteItem = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (remoteItem is not RemoteFolder)
        {
            Console.WriteLine(remoteItem);
            return;
        }
        DataAccessService.Instance.GetRemoteFolder(remoteItem.Id, out var remoteFiles, out var remoteFolders);
        foreach (var remoteFile in remoteFiles) Console.WriteLine(remoteFile);
        foreach (var remoteFolder in remoteFolders) Console.WriteLine(remoteFolder);
    }
}

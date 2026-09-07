using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands.Remote;

public class MoveCommand : Command
{
    public MoveCommand() : base("move", "Move or rename a remote item.")
    {
        AddAlias("mv");
        AddAlias("rename");
        var remoteFolderPathOption = new Option<string>("--into", "The path of the remote folder to move the item into.");
        var nameOption = new Option<string>(["--name", "-n"], "The new name to give the item.");
        AddArgument(DefaultParameters.pathArgument);
        AddOption(remoteFolderPathOption);
        AddOption(nameOption);
        AddOption(DefaultParameters.interactiveOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            remoteFolderPathOption,
            nameOption,
            DefaultParameters.interactiveOption
        );
    }

    private static void Handle(string path, string folderPath, string name, bool isInteractive)
    {
        var isRenameOnly = !string.IsNullOrEmpty(name) && string.IsNullOrEmpty(folderPath);
        var remoteItem = RemotePathResolver.ResolveItem(
            path,
            isInteractive,
            isRenameOnly ? "Select a remote item to rename." : "Select a remote item to move.",
            isRenameOnly ? "Rename this folder" : "Move this folder"
        );
        if (remoteItem == null) return;
        if (remoteItem.Id == DataAccessService.Instance.RootId) throw new Exception("Cannot move or rename the root folder");

        var service = DataAccessService.Instance;
        var result = remoteItem;
        if (!isRenameOnly)
        {
            var remoteFolder = RemotePathResolver.ResolveFolder(
                folderPath,
                isInteractive,
                $"Select a folder to move '{remoteItem.Name}' into:",
                "Move here"
            );
            if (remoteFolder == null) return;
            if (remoteFolder.Id == remoteItem.Id) throw new Exception("Cannot move a folder into itself");
            result = service.MoveRemoteItem(remoteItem.Id, remoteFolder.Id);
            Console.WriteLine($"Moved '{remoteItem.Name}' into '{remoteFolder.Name}'.");
        }
        if (!string.IsNullOrEmpty(name))
        {
            result = service.RenameRemoteItem(result.Id, name);
            Console.WriteLine($"Renamed '{remoteItem.Name}' to '{result.Name}'.");
        }
    }
}

using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands.Remote;

public class MoveCommand : Command
{
    public MoveCommand() : base("move", "Reparent an item.")
    {
        AddAlias("mv");
        var remoteFolderPathOption = new Option<string>("--into", "The path of the remote folder to move the item into.");
        AddArgument(DefaultParameters.pathArgument);
        AddOption(remoteFolderPathOption);
        AddOption(DefaultParameters.interactiveOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            remoteFolderPathOption,
            DefaultParameters.interactiveOption
        );
    }

    private static void Handle(string path, string folderPath, bool isInteractive)
    {
        RemoteItem remoteItem, remoteFolder;
        string defaultPath = "/";
        if (string.IsNullOrEmpty(path) || isInteractive)
        {
            if (string.IsNullOrEmpty(path)) path = defaultPath;
            var history = NavigationHelper.Navigate(path, new()
            {
                selectThisText = "Move this folder"
            });
            if (history == null) return;
            remoteItem = history.Peek();
            if (history.Peek() is not RemoteFolder) history.Pop();
            defaultPath = NavigationHelper.GetPathFromStack(history);
        }
        else remoteItem = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (remoteItem.Id == DataAccessService.Instance.RootId) throw new Exception("Cannot move root folder");
        if (string.IsNullOrEmpty(folderPath) || isInteractive)
        {
            if (string.IsNullOrEmpty(folderPath)) folderPath = defaultPath;
            remoteFolder = NavigationHelper.Navigate(
                folderPath,
                new()
                {
                    prompt = $"Select an folder to move '{remoteItem.Name}' into:",
                    selectThisText = "Move here",
                    onlyDisplayFolders = true
                }
            )?.Peek();
            if (remoteFolder == null) return;
        }
        else remoteFolder = DataAccessService.Instance.GetRemoteItemsFromPath(folderPath).Peek();
        if (remoteFolder is not RemoteFolder)
        {
            throw new Exception($"Folder path argument must be a remote folder. Remote item '{remoteFolder.Name}' ({remoteFolder.Id}) is not a folder");
        }
        DataAccessService.Instance.MoveRemoteItem(remoteItem.Id, remoteFolder.Id);
        Console.WriteLine($"Moved '{remoteItem.Name}' into '{remoteFolder.Name}'.");
    }
}
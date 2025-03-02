using System;
using System.CommandLine;
using GoogleDrivePushCli.Data.Models;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands.Remote;

public class MoveCommand : Command
{
    public MoveCommand() : base("move", "Reparent an item.")
    {
        AddAlias("mv");
        var folderPathArgument = new Argument<string>("folder-path", "The path of the remote folder to move the item into.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        AddArgument(DefaultParameters.pathArgument);
        AddArgument(folderPathArgument);
        AddOption(DefaultParameters.interactiveOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            folderPathArgument,
            DefaultParameters.interactiveOption
        );
    }

    private static void Handle(string path, string folderPath, bool isInteractive)
    {
        RemoteItem item, folder;
        string defaultPath = "/";
        if (string.IsNullOrEmpty(path) || isInteractive)
        {
            if (string.IsNullOrEmpty(path)) path = defaultPath;
            var history = NavigationHelper.Navigate(path, new()
            {
                selectThisText = "Move this"
            });
            if (history == null) return;
            item = history.Peek();
            if (!history.Peek().IsFolder) history.Pop();
            defaultPath = NavigationHelper.GetPathFromStack(history);
        }
        else item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        if (DriveServiceWrapper.Instance.IsRoot(item)) throw new Exception("Cannot move root folder");
        if (string.IsNullOrEmpty(folderPath) || isInteractive)
        {
            if (string.IsNullOrEmpty(folderPath)) folderPath = defaultPath;
            folder = NavigationHelper.Navigate(
                folderPath,
                new()
                {
                    prompt = $"Select an folder to move '{item.Name}' into:",
                    selectThisText = "Move here",
                    filterOnMimeType = DriveServiceWrapper.folderMimeType
                }
            )?.Peek();
            if (folder == null) return;
        }
        else folder = DriveServiceWrapper.Instance.GetItemsFromPath(folderPath).Peek();
        DriveServiceWrapper.Instance.MoveItem(item.Id, folder.Id);
        Console.WriteLine($"Moved '{item.Name}' into '{folder.Name}'.");
    }
}
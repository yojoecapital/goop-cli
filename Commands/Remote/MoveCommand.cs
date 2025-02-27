using System;
using System.CommandLine;
using Google.Apis.Drive.v3.Data;
using GoogleDrivePushCli.Utilities;

namespace GoogleDrivePushCli.Commands.Remote;

public class MoveCommand : Command
{
    public MoveCommand() : base("move", "Reparent an item.")
    {
        AddAlias("mv");
        var pathArgument = new Argument<string>("path", "The path of the remote item to move.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var folderPathArgument = new Argument<string>("folder-path", "The path of the remote folder to move the item into.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var interactiveOption = new Option<bool>(["--interactive", "-i"], "Open to the path and use an interactive prompt.");
        AddArgument(pathArgument);
        AddArgument(folderPathArgument);
        AddOption(interactiveOption);
        this.SetHandler(Handle, pathArgument, folderPathArgument, interactiveOption);
    }

    private static void Handle(string path, string folderPath, bool isInteractive)
    {
        File item, folder;
        string defaultPath = "/";
        if (string.IsNullOrEmpty(path) || isInteractive)
        {
            path ??= defaultPath;
            var history = NavigationHelper.Navigate(path, new()
            {
                selectThisText = "Move this"
            });
            if (history == null) return;
            item = history.Peek();
            if (!DriveServiceWrapper.IsFolder(history.Peek())) history.Pop();
            defaultPath = NavigationHelper.GetPathFromStack(history);
        }
        else item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        if (DriveServiceWrapper.Instance.IsRoot(item)) throw new Exception("Cannot move root folder");
        if (string.IsNullOrEmpty(folderPath) || isInteractive)
        {
            folderPath ??= defaultPath;
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
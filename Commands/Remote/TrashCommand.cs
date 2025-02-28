using System;
using System.CommandLine;
using GoogleDrivePushCli.Data.Models;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class TrashCommand : Command
{
    public TrashCommand() : base("trash", "Trash an item.")
    {
        var listOption = new Option<bool>(
            "--list",
            "List the items in the trash."
        );
        var emptyOption = new Option<bool>(
            "--empty",
            "Empty the trash."
        );
        AddArgument(DefaultParameters.pathArgument);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(listOption);
        AddOption(emptyOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            DefaultParameters.pathArgument,
            DefaultParameters.interactiveOption,
            listOption,
            emptyOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string path, bool isInteractive, bool shouldList, bool shouldEmpty, bool skipConfirmation)
    {
        // handle the path argument
        RemoteItem item = null;
        if (isInteractive || string.IsNullOrEmpty(path) && !shouldList && !shouldEmpty)
        {
            if (string.IsNullOrEmpty(path)) path = "/";
            item = NavigationHelper.Navigate(path, new()
            {
                selectThisText = "Trash this"
            }).Peek();
        }
        else if (!string.IsNullOrEmpty(path))
        {
            item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        }
        if (item != null)
        {
            if (DriveServiceWrapper.Instance.IsRoot(item))
            {
                throw new Exception("Cannot trash root folder");
            }
            DriveServiceWrapper.Instance.TrashItem(item.Id);
        }

        // handle the list option
        if (shouldList)
        {
            foreach (var listedItem in DriveServiceWrapper.Instance.GetItemsInTrash())
            {
                if (shouldEmpty) AnsiConsole.MarkupLineInterpolated($"[red]{listedItem}[/]");
                else Console.WriteLine(listedItem);
            }
        }

        // handle the empty option
        if (shouldEmpty)
        {
            if (skipConfirmation || AnsiConsole.Confirm("Are you sure you want to empty the trash?", false))
            {
                DriveServiceWrapper.Instance.EmptyTrash();
            }
        }
    }
}
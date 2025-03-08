using System;
using System.CommandLine;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class MakeFolderCommand : Command
{
    public MakeFolderCommand() : base("mkdir", "Make new folder in an existing remote folder.")
    {
        var pathArgument = new Argument<string>("path", "The path of the existing remote folder.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var nameOption = new Option<string>(["--name", "-n"], "The name of the new folder.");
        AddArgument(pathArgument);
        AddOption(nameOption);
        AddOption(DefaultParameters.interactiveOption);
        this.SetHandler(
            Handle,
            pathArgument,
            nameOption,
            DefaultParameters.interactiveOption
        );
    }

    private static void Handle(string path, string name, bool isInteractive)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        RemoteItem remoteFolder;
        if (isInteractive)
        {
            remoteFolder = NavigationHelper.Navigate(
                path,
                new()
                {
                    prompt = $"Select a location to create the folder.",
                    selectThisText = "Create here",
                    onlyDisplayFolders = true
                }
            )?.Peek();
            if (remoteFolder == null) return;
        }
        else remoteFolder = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (remoteFolder is not RemoteFolder)
        {
            throw new Exception($"Path argument must be a remote folder. Remote item '{remoteFolder.Name}' ({remoteFolder.Id}) is not a folder");
        }
        if (string.IsNullOrEmpty(name))
        {
            name = AnsiConsole.Prompt(new TextPrompt<string>("Enter the new folder's name:"));
        }
        AnsiConsole.Status().Start("Creating remote folder...", _ => DataAccessService.Instance.CreateEmptyRemoteFolder(remoteFolder.Id, name));
    }
}

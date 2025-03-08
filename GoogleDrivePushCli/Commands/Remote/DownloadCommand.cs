using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class DownloadCommand : Command
{
    public DownloadCommand() : base("download", "Download a remote item.")
    {
        var pathArgument = new Argument<string>("path", "The path of the remote item.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var localPathOption = new Option<string>("--to", "The local path to download the item to.");
        localPathOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        AddArgument(pathArgument);
        AddOption(localPathOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.depthOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            pathArgument,
            localPathOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.depthOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string path, string localPath, bool isInteractive, int depth, bool skipConfirmation)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        RemoteItem remoteItem;
        if (isInteractive)
        {
            remoteItem = NavigationHelper.Navigate(
                path,
                new()
                {
                    prompt = $"Select a remote item to download."
                }
            )?.Peek();
            if (remoteItem == null) return;
        }
        else remoteItem = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (Directory.Exists(localPath))
        {
            localPath = Path.Join(localPath, remoteItem.Name);
        }
        if (remoteItem is RemoteFile remoteFile)
        {
            if (
                File.Exists(localPath) &&
                !skipConfirmation &&
                !AnsiConsole.Confirm($"A local file already exists at '{localPath}'. Replace it?", false)
            ) return;
            AnsiConsole.Progress().Start(context =>
            {
                var task = context.AddTask($"Downloading '{remoteFile.Name}'", maxValue: 1);
                OperationHelpers.Run(progress => DataAccessService.Instance.DownloadFile(remoteFile, localPath, progress), task);
            });
            Console.WriteLine($"Downloaded remote file '{remoteFile.Name}' ({remoteFile.Id}) to '{localPath}'.");
        }
        else if (remoteItem is RemoteFolder remoteFolder)
        {
            if (Directory.Exists(localPath)) throw new Exception($"A local folder already exists at '{localPath}'");
            AnsiConsole.Progress().Start(context =>
            {
                var task = context.AddTask($"Downloading '{remoteFolder.Name}'", maxValue: 1);
                OperationHelpers.Run(progress => DataAccessService.Instance.DownloadFolder(remoteFolder, localPath, depth, progress), task);
            });
            Console.WriteLine($"Downloaded remote folder '{remoteFolder.Name}' ({remoteFolder.Id}) to '{localPath}'.");
        }
    }
}

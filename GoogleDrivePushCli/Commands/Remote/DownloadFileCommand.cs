using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class DownloadFileCommand : Command
{
    public DownloadFileCommand() : base("download", "Download a remote file.")
    {
        var pathArgument = new Argument<string>("path", "The path of the remote file.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var localPathOption = new Option<string>("--to", "The local path to download the file to.");
        localPathOption.SetDefaultValueFactory(Directory.GetCurrentDirectory);
        AddArgument(pathArgument);
        AddOption(localPathOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            pathArgument,
            localPathOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string path, string localPath, bool isInteractive, bool skipConfirmation)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        RemoteItem remoteFile;
        if (isInteractive)
        {
            remoteFile = NavigationHelper.Navigate(
                path,
                new()
                {
                    prompt = $"Select a remote file to download.",
                    onlyDisplayFiles = true
                }
            )?.Peek();
            if (remoteFile == null) return;
        }
        else remoteFile = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (remoteFile is not RemoteFile)
        {
            throw new Exception($"Path argument must be a remote file. Remote item '{remoteFile.Name}' ({remoteFile.Id}) is not a file");
        }
        if (Directory.Exists(localPath))
        {
            localPath = Path.Join(localPath, remoteFile.Name);
        }
        if (
            File.Exists(localPath) &&
            !skipConfirmation &&
            !AnsiConsole.Confirm($"A file already exists at '{localPath}'. Replace it?", false)
        ) return;
        AnsiConsole.Status().Start($"Downloading '{remoteFile.Name}'...", _ => DataAccessService.Instance.DownloadFile(remoteFile.Id, localPath));
        Console.WriteLine($"Downloaded remote file '{remoteFile.Name}' ({remoteFile.Id}) to '{localPath}'.");
    }
}

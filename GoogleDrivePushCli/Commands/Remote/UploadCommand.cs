using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class UploadCommand : Command
{
    public UploadCommand() : base("upload", "Upload a local file or folder into a remote folder.")
    {
        AddAlias("up");
        var localPathArgument = new Argument<string>("local-path", "The path of the local file or folder to upload.");
        var remotePathOption = new Option<string>("--into", "The path of the remote folder to upload into.");
        AddArgument(localPathArgument);
        AddOption(remotePathOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.depthOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            localPathArgument,
            remotePathOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.depthOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string localPath, string remoteFolderPath, bool isInteractive, int depth, bool skipConfirmation)
    {
        localPath = Path.GetFullPath(localPath);
        var isDirectory = Directory.Exists(localPath);
        if (!isDirectory && !File.Exists(localPath))
        {
            throw new FileNotFoundException($"No local file or folder exists at '{localPath}'");
        }
        var remoteFolder = RemotePathResolver.ResolveFolder(
            remoteFolderPath,
            isInteractive,
            $"Select a remote folder to upload '{Path.GetFileName(localPath)}' into:",
            "Upload here"
        );
        if (remoteFolder == null) return;
        var name = Path.GetFileName(localPath);
        if (!skipConfirmation && !AnsiConsole.Confirm($"Upload '{name}' into '{remoteFolder.Name}'?", true)) return;
        var service = DataAccessService.Instance;
        AnsiConsole.Progress().Start(context =>
        {
            var task = context.AddTask($"Uploading '{name}'", maxValue: 1);
            OperationHelpers.Run(
                progress =>
                {
                    if (isDirectory) service.CreateRemoteFolder(remoteFolder.Id, localPath, depth, progress);
                    else service.CreateRemoteFile(remoteFolder.Id, localPath, progress);
                },
                task
            );
        });
        Console.WriteLine($"Uploaded '{localPath}' into '{remoteFolder.Name}' ({remoteFolder.Id}).");
    }
}

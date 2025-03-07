using System;
using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class UploadFileCommand : Command
{
    public UploadFileCommand() : base("upload", "Upload a local file into a remote folder.")
    {
        var localPathArgument = new Argument<string>("path", "The path of the local file.");
        var remoteFolderPathOption = new Option<string>("--into", "The path of the remote folder to upload the file into.");
        AddArgument(localPathArgument);
        AddOption(remoteFolderPathOption);
        AddOption(DefaultParameters.interactiveOption);
        AddOption(DefaultParameters.yesOption);
        this.SetHandler(
            Handle,
            localPathArgument,
            remoteFolderPathOption,
            DefaultParameters.interactiveOption,
            DefaultParameters.yesOption
        );
    }

    private static void Handle(string localPath, string remoteFolderPath, bool isInteractive, bool skipConfirmation)
    {
        if (Directory.Exists(localPath))
        {
            throw new Exception($"The local path argument must be a path. Local item '{localPath}' is directory");
        }
        if (!File.Exists(localPath))
        {
            throw new Exception($"The file '{localPath}' does not exist");
        }
        if (string.IsNullOrEmpty(remoteFolderPath))
        {
            isInteractive = true;
            remoteFolderPath = "/";
        }
        RemoteItem remoteFolder;
        if (isInteractive)
        {
            remoteFolder = NavigationHelper.Navigate(
                remoteFolderPath,
                new()
                {
                    prompt = $"Select a remote folder to upload the file into.",
                    onlyDisplayFolders = true
                }
            )?.Peek();
            if (remoteFolder == null) return;
        }
        else remoteFolder = DataAccessService.Instance.GetRemoteItemsFromPath(remoteFolderPath).Peek();
        if (remoteFolder is not RemoteFolder)
        {
            throw new Exception($"Remote path argument must be a remote folder. Remote item '{remoteFolder.Name}' ({remoteFolder.Id}) is not a folder");
        }
        var fileName = Path.GetFileName(localPath);
        try
        {
            var possibleDuplicateFile = $"{remoteFolderPath}/{fileName}";
            DataAccessService.Instance.GetRemoteItemsFromPath(possibleDuplicateFile).Peek();
        }
        catch (FileNotFoundException)
        {
            if (
                !skipConfirmation &&
                !AnsiConsole.Confirm($"A remote file named '{fileName}' already exists in '{remoteFolder.Name}' ({remoteFolder.Id}). Upload a duplicate?", false)
            ) return;
        }
        AnsiConsole.Status().Start($"Uploading '{fileName}'...", _ => DataAccessService.Instance.UpdateRemoteFile(remoteFolder.Id, localPath));
        Console.WriteLine($"Uploaded '{fileName}' to remote folder '{remoteFolder.Name}' ({remoteFolder.Id}).");
    }
}

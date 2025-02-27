using System.CommandLine;
using Google.Apis.Drive.v3.Data;
using GoogleDrivePushCli.Utilities;
using Spectre.Console;

namespace GoogleDrivePushCli.Commands.Remote;

public class InformationCommand : Command
{
    public InformationCommand() : base("information", "Get information for a remote item.")
    {
        AddAlias("info");
        var pathArgument = new Argument<string>("path", "The path of the item  to retrieve.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };
        var interactiveOption = new Option<bool>(["--interactive", "-i"], "Open to the path and use an interactive prompt.");
        AddArgument(pathArgument);
        AddOption(interactiveOption);
        this.SetHandler(Handle, pathArgument, interactiveOption);
    }

    private static void Handle(string path, bool isInteractive)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        File item;
        if (isInteractive)
        {
            item = NavigationHelper.Navigate(path)?.Peek();
            if (item == null) return;
        }
        else item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddRow(["[bold]ID[/]", $": {item.Id}"]);
        grid.AddRow(["[bold]Name[/]", $": {item.Name.EscapeMarkup()}"]);
        grid.AddRow(["[bold]MIME type[/]", $": {item.MimeType}"]);
        grid.AddRow(["[bold]Modified time[/]", $": {item.ModifiedTimeRaw}"]);
        if (item.Size.HasValue) grid.AddRow(["[bold]Size[/]", $": {item.Size.Value.ToFileSize()}"]);
        AnsiConsole.Write(grid);
    }
}

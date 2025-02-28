using System;
using System.CommandLine;

namespace GoogleDrivePushCli.Commands.Remote;

public class ListCommand : Command
{
    public ListCommand() : base("list", "List the items in a remote folder.")
    {
        AddAlias("ls");
        var pathArgument = new Argument<string>("path", "The path of the remote folder.");
        pathArgument.SetDefaultValue("/");
        AddArgument(pathArgument);
        this.SetHandler(
            Handle,
            pathArgument
        );
    }

    private static void Handle(string path)
    {
        var item = DriveServiceWrapper.Instance.GetItemsFromPath(path).Peek();
        if (!item.IsFolder)
        {
            Console.WriteLine(item);
            return;
        }
        foreach (var child in DriveServiceWrapper.Instance.GetItems(item.Id, out var _))
        {
            Console.WriteLine(child);
        }
    }
}

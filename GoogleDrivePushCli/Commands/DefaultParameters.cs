using System.CommandLine;
using System.IO;
using GoogleDrivePushCli.Json.Configuration;

namespace GoogleDrivePushCli.Commands
{
    public static class DefaultParameters
    {
        public static readonly Option<bool> interactiveOption = new(
            ["--interactive", "-i"],
            "Open to the path and use an interactive prompt."
        );

        public static readonly Option<bool> yesOption = new(
            ["--yes", "-y"],
            "Skip the confirmation prompt."
        );

        public static readonly Argument<string> pathArgument = new(
            "path",
            "The path of the remote item."
        )
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        public static readonly Option<string> workingDirectoryOption = new(
            "--working-dir",
            Directory.GetCurrentDirectory,
            "The working directory to use."
        );

        public static readonly Option<string> operationsOption = new(
            ["--operations", "-x"],
            () => "cud",
            @"The operations to perform, represented as a string. 
'c' stands for create, 'u' for update, and 'd' for delete."
        );

        public static readonly Option<string[]> ignoreOption = new(
            ["--ignore", "-i"],
            "Ignore additional paths from being processed."
        )
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        public static readonly Option<int> depthOption = new(
            ["--depth", "-d"],
            () => ApplicationConfiguration.Instance.DefaultDepth,
            "The depth that to sync the folder at."
        );
    }
}
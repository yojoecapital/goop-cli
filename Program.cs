
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;

namespace GoogleDrivePushCli
{
    internal static partial class Program
    {
        private static void InitializeProgram(bool verbose)
        {
            Logger.verbose = verbose;
            Directory.CreateDirectory(Defaults.configurationPath);
        }


        [STAThread]
        public static int Main(string[] args)
        {
            // Top level options
            var workingDirectoryOption = new Option<string>
            (
                ["--directory", "-wd"],
                GetEnvironmentCurrentDirectory,
                "The working directory."
            )
            {
                IsRequired = false,
            };
            var verboseOption = new Option<bool>(["--verbose", "-vb"], "Show [INFO] messages.");

            // Initialize command
            var folderIdArgument = new Argument<string>("folderId", "The folder ID to initialize.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            var depthOption = new Option<int>(["--depth", "-d"], "The depth that to sync the folder at.");
            depthOption.SetDefaultValue(3);
            var initializeCommand = new Command("initialize", "Initializes the Google Drive synchronization.")
            {
                folderIdArgument,
                depthOption
            };
            initializeCommand.AddAlias("init");
            initializeCommand.SetHandler(InitializeHandler, workingDirectoryOption, verboseOption, folderIdArgument, depthOption);

            // Push command
            var confirmOption = new Option<bool>(["--yes", "-y"], "Confirm the action. Otherwise, only the potential changes are shown.");
            var pushCommand = new Command("push", "Pushes local changes to Google Drive.")
            {
                confirmOption
            };
            pushCommand.SetHandler(PushHandler, workingDirectoryOption, verboseOption, confirmOption);

            // Pull command
            var pullCommand = new Command("pull", "Pulls remote changes from Google Drive.")
            {
                confirmOption
            };
            pullCommand.SetHandler(PullHandler, workingDirectoryOption, verboseOption, confirmOption);

            // Fetch command
            var fetchCommand = new Command("fetch", $"Updates the Google Drive cached in '{Defaults.metadataFileName}'.");
            fetchCommand.SetHandler(FetchHandler, workingDirectoryOption, verboseOption);

            // Info command
            var infoCommand = new Command("info", "Outputs information about synced folder.");
            infoCommand.SetHandler(InfoHandler, workingDirectoryOption, verboseOption);

            // Ignore command
            var pathArgument = new Argument<string>("path", "Path of the file.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            var addOption = new Option<bool>(["--add", "-a"], "Adds the value.");
            addOption.SetDefaultValue(true);
            var removeOption = new Option<bool>(["--remove", "-rm"], "Removes the value.");
            var ignoreCommand = new Command("ignore", "Remove or add a file to the ignored list specified on the path.")
            {
                pathArgument,
                addOption,
                removeOption
            };
            ignoreCommand.SetHandler(IgnoreHandler, workingDirectoryOption, verboseOption, pathArgument, addOption, removeOption);

            // Depth command
            var depthArgument = new Argument<int?>("depth", "The depth value.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
            var depthCommand = new Command("depth", "Updates the max depth the sync folder uses.")
            {
                depthArgument
            };
            depthCommand.SetHandler(DepthHandler, workingDirectoryOption, verboseOption, depthArgument);

            // Application root
            var rootCommand = new RootCommand($"The {Defaults.applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
            {
                initializeCommand,
                fetchCommand,
                pushCommand,
                pullCommand,
                ignoreCommand,
                depthCommand,
                infoCommand,
            };
            rootCommand.AddGlobalOption(workingDirectoryOption);
            rootCommand.AddGlobalOption(verboseOption);

            // Run the application
            var cli = new CommandLineBuilder(rootCommand)
                .UseHelp()
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
                .UseVersionOption("--version", "-v")
                .Build();
            return cli.Invoke(args);
        }

        private static string GetEnvironmentCurrentDirectory() => Environment.CurrentDirectory;

        private static void ExceptionHandler(Exception ex, InvocationContext context)
        {
            Logger.Error(ex.Message);
            context.ExitCode = 1;
        }
    }
}
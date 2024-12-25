
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
                "The working directory (default is the current directory)."
            )
            {
                IsRequired = false,
            };
            var verboseOption = new Option<bool>(["--verbose", "-vb"], "Show [INFO] messages.");

            // Initialize command
            var folderIdOption = new Option<string>(["--folderId", "-fid"], "The folder ID to initialize.")
            {
                IsRequired = true
            };
            var depthOption = new Option<int>(["--depth", "-d"], "The depth that to sync the folder at.")
            {
                IsRequired = true
            };
            var initializeCommand = new Command("initialize", "Initializes the Google Drive synchronization.")
            {
                folderIdOption,
                depthOption
            };
            initializeCommand.AddAlias("init");
            initializeCommand.SetHandler(InitializeHandler, workingDirectoryOption, verboseOption, folderIdOption, depthOption);

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
            var ignoreCommand = new Command("ingore", "Ignores the file specified on the path.");
            var pathArgument = new Argument<string>("path", "Path of the file.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            ignoreCommand.AddArgument(pathArgument);
            ignoreCommand.SetHandler(IgnoreHandler, workingDirectoryOption, verboseOption, pathArgument);

            // Track command
            var trackCommand = new Command("track", "Un-ignores the file specified on the path.");
            trackCommand.AddArgument(pathArgument);
            trackCommand.SetHandler(TrackHandler, workingDirectoryOption, verboseOption, pathArgument);

            // Depth command
            var depthCommand = new Command("depth", "Updates the max depth the sync folder uses.");
            var depthArgument = new Argument<int>("depth", "The depth value.")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            depthCommand.AddArgument(depthArgument);
            depthCommand.SetHandler(DepthHandler, workingDirectoryOption, verboseOption, depthArgument);

            // Application root
            var rootCommand = new RootCommand($"The {Defaults.applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
            {
                initializeCommand,
                pushCommand,
                pullCommand,
                fetchCommand
            };
            rootCommand.AddGlobalOption(workingDirectoryOption);
            rootCommand.AddGlobalOption(verboseOption);

            // Run the application
            var cli = new CommandLineBuilder(rootCommand)
                .UseHelp()
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
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
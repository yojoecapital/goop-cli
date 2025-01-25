
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using GoogleDrivePushCli.Commands;

namespace GoogleDrivePushCli
{
    public static partial class Program
    {
        public static readonly string version = "2.2.0";
        private static readonly Option<bool> verboseOption = new(["--verbose", "-vb"], "Show [INFO] messages.");

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

            // Id command
            var idPathArgument = new Argument<string>("path", "The path of the remote item.");
            idPathArgument.SetDefaultValue("/");
            var navigateOption = new Option<bool>(["--navigate", "-nav"], "Select the item via an interactive prompt");
            var idCommand = new Command("id", "Teturn the Google Drive id for an item")
            {
                idPathArgument,
                navigateOption
            };
            idCommand.SetHandler(IdHandler.Handle, idPathArgument, navigateOption);

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
            initializeCommand.SetHandler(InitHandler.Handle, workingDirectoryOption, folderIdArgument, depthOption);

            // Push command
            var noFetchOption = new Option<bool>("--no-fetch", "Do not call fetch and solely rely on cache.");
            var confirmOption = new Option<bool>(["--yes", "-y"], "Confirm the action. Otherwise, only the potential changes are shown.");
            var pushCommand = new Command("push", "Pushes local changes to Google Drive.")
            {
                noFetchOption,
                confirmOption
            };
            pushCommand.SetHandler(PushHandler.Handle, workingDirectoryOption, noFetchOption, confirmOption);

            // Pull command
            var pullCommand = new Command("pull", "Pulls remote changes from Google Drive.")
            {
                noFetchOption,
                confirmOption
            };
            pullCommand.SetHandler(PullHandler.Handle, workingDirectoryOption, noFetchOption, confirmOption);

            // Fetch command
            var fetchCommand = new Command("fetch", $"Updates the Google Drive cached in '{Defaults.metadataFileName}'.");
            fetchCommand.SetHandler(FetchHandler.Handle, workingDirectoryOption);

            // Depth command
            var depthArgument = new Argument<int?>("depth", "The depth value.")
            {
                Arity = ArgumentArity.ZeroOrOne
            };
            var depthCommand = new Command("depth", "Updates the max depth the sync folder uses.")
            {
                depthArgument
            };
            depthCommand.SetHandler(DepthHandler.Handle, workingDirectoryOption, depthArgument);

            // Info command
            var infoCommand = new Command("info", "Outputs information about sync folder.");
            infoCommand.SetHandler(InfoHandler.Handle, workingDirectoryOption);

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
            ignoreCommand.SetHandler(IgnoreHandler.Handle, workingDirectoryOption, pathArgument, addOption, removeOption);


            // Application root
            var rootCommand = new RootCommand($"The {Defaults.applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
            {
                idCommand,
                initializeCommand,
                fetchCommand,
                pushCommand,
                pullCommand,
                ignoreCommand,
                depthCommand,
                infoCommand
            };
            rootCommand.AddGlobalOption(workingDirectoryOption);
            rootCommand.AddGlobalOption(verboseOption);

            // Run the application
            var cli = new CommandLineBuilder(rootCommand)
                .AddMiddleware(InitializeHandler)
                .UseHelp()
                .AddMiddleware(VersionHandler)
                .UseParseErrorReporting()
                .UseExceptionHandler(ExceptionHandler)
                .Build();

            return cli.Invoke(args);
        }

        private static string GetEnvironmentCurrentDirectory() => Environment.CurrentDirectory;

        private static Task InitializeHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            Logger.Verbose = context.ParseResult.GetValueForOption(verboseOption);
            Directory.CreateDirectory(Defaults.configurationPath);
            return next(context);
        }

        private static Task VersionHandler(InvocationContext context, Func<InvocationContext, Task> next)
        {
            var tokens = context.ParseResult.Tokens;
            if (tokens.Count != 1) return next(context);
            var firstToken = tokens[0].ToString();
            if (firstToken == "-v" || firstToken == "--version" || firstToken == "version")
            {
                Logger.Message(version);
                return Task.CompletedTask;
            }
            return next(context);
        }

        private static void ExceptionHandler(Exception ex, InvocationContext context)
        {
            Logger.Error(ex.Message);
#if DEBUG
            Console.WriteLine(ex.StackTrace);
#endif
            context.ExitCode = 1;
        }
    }
}
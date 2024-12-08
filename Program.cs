
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;

internal partial class Program
{
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
        var initializeCommand = new Command("initialize", "Initializes the Google Drive synchronization.")
        {   
            folderIdOption
        };
        initializeCommand.AddAlias("init");
        initializeCommand.SetHandler(InitializeHandler, workingDirectoryOption, verboseOption, folderIdOption);

        // Push command
        var confirmOption = new Option<bool>(["--yes", "-y"], "Confirm the action. Otherwise, only the potential changes are shown.");
        var pushCommand = new Command("push", "Pushes local changes to Google Drive.")
        {
            confirmOption
        };
        pushCommand.SetHandler(PushHandler, workingDirectoryOption, verboseOption, confirmOption);

        // Application root
        var rootCommand = new RootCommand($"The {applicationName} is a simple tool for syncing files between a local directory and Google Drive.")
        {
            initializeCommand,
            pushCommand
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
        WriteError(ex.Message);
        context.ExitCode = 1;
    }
}
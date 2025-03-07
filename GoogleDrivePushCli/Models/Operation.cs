using System;
using Spectre.Console;

namespace GoogleDrivePushCli.Models;

public class Operation
{
    private readonly string description;
    private readonly Action<IProgress<double>> action;

    public Operation(string description, Action<IProgress<double>> action)
    {
        this.description = description;
        this.action = action;
    }

    public Operation(string description, Action action)
    {
        this.description = description;
        this.action = progress =>
        {
            action.Invoke();
            progress.Report(1);
        };
    }

    public string Description => description;
    public Action<IProgress<double>> Action => action;

    public void Run(ProgressTask progressTask) => Run(action, progressTask);

    public static void Run(Action<IProgress<double>> action, ProgressTask progressTask)
    {
        double previous = 0;
        var progress = new Progress<double>(percent =>
        {
            var change = percent - previous;
            progressTask.Increment(change);
            previous = percent;
        });
        action.Invoke(progress);
        progressTask.Increment(1 - previous);
    }
}
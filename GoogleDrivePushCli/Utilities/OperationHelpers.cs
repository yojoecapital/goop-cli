using System;
using System.Collections.Generic;
using GoogleDrivePushCli.Models;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities;

public static class OperationHelpers
{
    public static void PromptAndRun(
        List<Operation> createOperations,
        List<Operation> updateOperations,
        List<Operation> deleteOperations,
        bool skipConfirmation
    )
    {
        var operationCount = createOperations.Count + updateOperations.Count + deleteOperations.Count;
        if (operationCount == 0)
        {
            Console.WriteLine("Up to date.");
            return;
        }
        foreach (var operation in createOperations) AnsiConsole.MarkupLineInterpolated($"[green][[CREATE]][/] {operation.Description}");
        foreach (var operation in updateOperations) AnsiConsole.MarkupLineInterpolated($"[yellow][[UPDATE]][/] {operation.Description}");
        foreach (var operation in deleteOperations) AnsiConsole.MarkupLineInterpolated($"[red][[DELETE]][/] {operation.Description}");
        if (!skipConfirmation && !AnsiConsole.Confirm($"Execute [bold]{operationCount}[/] operation(s)?", false))
        {
            return;
        }

        AnsiConsole.Progress().Start(context =>
        {
            var createTask = createOperations.Count > 0 ? context.AddTask("[green]Creating items[/]", maxValue: createOperations.Count) : null;
            var updateTask = updateOperations.Count > 0 ? context.AddTask("[yellow]Updating items[/]", maxValue: updateOperations.Count) : null;
            var deleteTask = deleteOperations.Count > 0 ? context.AddTask("[red]Deleting items[/]", maxValue: updateOperations.Count) : null;
            foreach (var operation in createOperations)
            {
                Run(operation.Action, createTask);
            }
            foreach (var operation in updateOperations)
            {
                Run(operation.Action, updateTask);
            }
            foreach (var operation in deleteOperations)
            {
                Run(operation.Action, deleteTask);
            }
        });
    }

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
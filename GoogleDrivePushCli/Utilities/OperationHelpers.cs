using System;
using System.Collections.Generic;
using System.Linq;
using GoogleDrivePushCli.Models;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities;

public static class OperationHelpers
{
    public static HashSet<OperationType> GetAllowedOperationTypes(string operations)
    {
        if (string.IsNullOrWhiteSpace(operations)) throw new ArgumentException("No operations were specified");
        var characters = operations.ToLower().ToHashSet();
        var set = new HashSet<OperationType>();
        if (characters.Remove('c')) set.Add(OperationType.Create);
        if (characters.Remove('u')) set.Add(OperationType.Update);
        if (characters.Remove('d')) set.Add(OperationType.Delete);
        if (characters.Count > 0) throw new ArgumentException($"Unrecognized operation: '{characters.First()}'");
        return set;
    }

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

        var failures = new List<string>();
        AnsiConsole.Progress().Start(context =>
        {
            RunAll(createOperations, context, "Creating items", failures);
            RunAll(updateOperations, context, "Updating items", failures);
            RunAll(deleteOperations, context, "Deleting items", failures);
        });
        foreach (var failure in failures) ConsoleHelpers.Error(failure);
        if (failures.Count > 0) throw new Exception($"{failures.Count} of {operationCount} operation(s) failed");
    }

    private static void RunAll(List<Operation> operations, ProgressContext context, string description, List<string> failures)
    {
        if (operations.Count == 0) return;
        var task = context.AddTask(description, maxValue: operations.Count);
        var completed = 0;
        foreach (var operation in operations)
        {
            try
            {
                RunFrom(operation.Action, task, completed);
            }
            catch (Exception exception)
            {
                failures.Add($"{operation.Description} {exception.Message}");
            }
            completed++;
            task.Value = completed;
        }
    }

    public static void Run(Action<IProgress<double>> action, ProgressTask progressTask)
    {
        RunFrom(action, progressTask, progressTask.Value);
        progressTask.Value = progressTask.MaxValue;
    }

    private static void RunFrom(Action<IProgress<double>> action, ProgressTask progressTask, double start)
    {
        action.Invoke(new SynchronousProgress(percent => progressTask.Value = start + Math.Clamp(percent, 0, 1)));
    }
}

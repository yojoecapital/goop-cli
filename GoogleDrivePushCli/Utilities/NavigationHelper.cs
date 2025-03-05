using System;
using System.Collections.Generic;
using System.Linq;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities
{

    public static class NavigationHelper
    {
        public class Configuration
        {
            public string prompt;
            public string selectThisText;
            public bool onlyDisplayFolders;
            public bool onlyDisplayFiles;

            public Configuration()
            {
                prompt = "Select an item:";
                selectThisText = "Select this folder";
            }
        }


        private enum ActionType
        {
            GoUp,
            File,
            SelectThis,
            Folder,
            Cancel
        }

        private class NavigationChoice
        {

            public ActionType Action { get; private set; }
            public string Display { get; private set; }
            public RemoteItem RemoteItem { get; private set; }

            public NavigationChoice(RemoteItem remoteItem)
            {
                RemoteItem = remoteItem;
                if (remoteItem is RemoteFolder)
                {
                    Action = ActionType.Folder;
                    Display = $"[[+]] {remoteItem.Name.EscapeMarkup()}";
                }
                else
                {
                    Action = ActionType.File;
                    Display = $"[[-]] {remoteItem.Name.EscapeMarkup()}";
                }
            }

            private NavigationChoice() { }

            public override string ToString() => Display;

            public static readonly NavigationChoice goUp = new()
            {
                Action = ActionType.GoUp,
                Display = "[yellow][[↑]] Go up[/]"
            };

            public static readonly NavigationChoice cancel = new()
            {
                Action = ActionType.Cancel,
                Display = "[red][[X]] Cancel[/]"
            };

            public static NavigationChoice SelectThis(RemoteItem remoteItem, string selectThis) => new()
            {
                RemoteItem = remoteItem,
                Action = ActionType.SelectThis,
                Display = $"[[-]] {selectThis}",
            };
        }

        public static string GetPathFromStack(Stack<RemoteItem> stack) => string.Join('/', stack.Select(item => item.Name).Reverse());

        public static Stack<RemoteItem> Navigate(string path, Configuration configuration = null)
        {
            var history = DataAccessManager.Instance.GetRemoteItemsFromPath(path);
            if (history.Count == 0) throw new Exception("Nothing to navigate");
            if (history.Peek() is not RemoteFolder)
            {
                if (history.Count == 1) throw new Exception("Cannot navigate from a file");
                history.Pop();
            }
            configuration ??= new();
            while (true)
            {
                var currentRow = Console.CursorTop;
                var choices = new List<NavigationChoice>();
                if (history.Count > 1) choices.Add(NavigationChoice.goUp);
                if (!configuration.onlyDisplayFiles) choices.Add(NavigationChoice.SelectThis(history.Peek(), configuration.selectThisText));
                DataAccessManager.Instance.GetRemoteFolder(history.Peek().Id, out var remoteFiles, out var remoteFolders);
                if (!configuration.onlyDisplayFolders) choices.AddRange(remoteFiles.Select(remoteFiles => new NavigationChoice(remoteFiles)));
                choices.AddRange(remoteFolders.Select(remoteFolder => new NavigationChoice(remoteFolder)));
                choices.Add(NavigationChoice.cancel);
                var currentPath = GetPathFromStack(history);
                var prompt = new SelectionPrompt<NavigationChoice>()
                    .Title($"{configuration.prompt.EscapeMarkup()} [[[yellow]{currentPath.EscapeMarkup()}[/]]]")
                    .PageSize(10)
                    .WrapAround()
                    .AddChoices(choices);
                var choice = AnsiConsole.Prompt(prompt);
                switch (choice.Action)
                {
                    case ActionType.GoUp:
                        history.Pop();
                        break;
                    case ActionType.Cancel:
                        return null;
                    case ActionType.Folder:
                        history.Push(choice.RemoteItem);
                        break;
                    case ActionType.File:
                        history.Push(choice.RemoteItem);
                        return history;
                    case ActionType.SelectThis:
                        return history;
                }
                ConsoleHelpers.Clear(currentRow);
            }
        }
    }
}
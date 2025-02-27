using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Drive.v3.Data;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities
{

    public static class NavigationHelper
    {
        public struct Configuration
        {
            public string prompt;
            public string selectThisText;
            public string filterOnMimeType;

            public Configuration()
            {
                prompt = "Select an itme:";
                selectThisText = "Select this";
                filterOnMimeType = null;
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
            public File Item { get; private set; }

            public NavigationChoice(File item)
            {
                Item = item;
                if (DriveServiceWrapper.IsFolder(item))
                {
                    Action = ActionType.Folder;
                    Display = $"[[+]] {item.Name.EscapeMarkup()}";
                }
                else
                {
                    Action = ActionType.File;
                    Display = $"[[-]] {item.Name.EscapeMarkup()}";
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

            public static NavigationChoice SelectThis(File item, string selectThis) => new()
            {
                Item = item,
                Action = ActionType.SelectThis,
                Display = $"[[-]] {selectThis}",
            };
        }

        public static string GetPathFromStack(Stack<File> stack) => string.Join('/', stack.Select(item => item.Name).Reverse());

        public static Stack<File> Navigate(string path, Configuration? configuration = null)
        {
            var history = DriveServiceWrapper.Instance.GetItemsFromPath(path);
            if (history.Count == 0) throw new Exception("Nothing to navigate");
            if (!configuration.HasValue) configuration = new();
            while (true)
            {
                var currentRow = Console.CursorTop;
                var choices = new List<NavigationChoice>();
                if (history.Count > 1) choices.Add(NavigationChoice.goUp);
                choices.Add(NavigationChoice.SelectThis(history.Peek(), configuration.Value.selectThisText));
                IEnumerable<File> items = DriveServiceWrapper.Instance.GetItems(history.Peek().Id, out var _);
                if (configuration.Value.filterOnMimeType != null) items = items.Where(item => item.MimeType == configuration.Value.filterOnMimeType);
                choices.AddRange(items.Select(item => new NavigationChoice(item)));
                choices.Add(NavigationChoice.cancel);
                var currentPath = GetPathFromStack(history);
                var prompt = new SelectionPrompt<NavigationChoice>()
                    .Title($"{configuration.Value.prompt.EscapeMarkup()} [[[yellow]{currentPath.EscapeMarkup()}[/]]]")
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
                        history.Push(choice.Item);
                        break;
                    case ActionType.File:
                        history.Push(choice.Item);
                        return history;
                    case ActionType.SelectThis:
                        return history;
                }
                ConsoleHelpers.Clear(currentRow);
            }
        }
    }
}
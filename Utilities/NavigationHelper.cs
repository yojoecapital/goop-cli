using System;
using System.Collections.Generic;
using System.Linq;
using Google.Apis.Drive.v3.Data;
using Spectre.Console;

namespace GoogleDrivePushCli.Utilities
{
    public static class NavigationHelper
    {
        private enum ActionType
        {
            GoUp,
            Select,
            Enter,
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
                    Action = ActionType.Enter;
                    Display = $"[[+]] {item.Name.EscapeMarkup()}";
                }
                else
                {
                    Action = ActionType.Select;
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

            public static NavigationChoice Select(File item, string selectThis) => new()
            {
                Item = item,
                Action = ActionType.Select,
                Display = $"[[-]] {selectThis}",
            };
        }
        public static File Navigate(string title, string path, string selectThis)
        {
            var history = DriveServiceWrapper.Instance.GetItemsFromPath(path);
            if (history.Count == 0) throw new Exception("Nothing to navigate");
            while (true)
            {
                var currentRow = Console.CursorTop;
                var choices = new List<NavigationChoice>();
                if (history.Count > 1) choices.Add(NavigationChoice.goUp);
                choices.Add(NavigationChoice.Select(history.Peek(), selectThis));
                choices.AddRange(DriveServiceWrapper.Instance.GetItems(history.Peek().Id, out var _).Select(item => new NavigationChoice(item)));
                choices.Add(NavigationChoice.cancel);
                var currentPath = string.Join('/', history.Select(item => item.Name));
                var prompt = new SelectionPrompt<NavigationChoice>()
                    .Title($"{title.EscapeMarkup()} [[[yellow]{currentPath.EscapeMarkup()}[/]]]")
                    .PageSize(10)
                    .AddChoices(choices);
                var choice = AnsiConsole.Prompt(prompt);
                switch (choice.Action)
                {
                    case ActionType.GoUp:
                        history.Pop();
                        break;
                    case ActionType.Cancel:
                        return null;
                    case ActionType.Enter:
                        history.Push(choice.Item);
                        break;
                    case ActionType.Select:
                        return choice.Item;
                }
                ConsoleHelpers.Clear(currentRow);
            }
        }
    }
}
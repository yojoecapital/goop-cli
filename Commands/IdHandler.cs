using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogleDrivePushCli.Commands
{
    public static class IdHandler
    {
        public static void Handle(string path, bool navigate)
        {
            if (navigate) Logger.Message(NavigateAndGetId(path.Trim()));
            else Logger.Message(GetIdFromPath(path.Trim(), out var _));
        }

        public static string NavigateAndGetId(string path)
        {
            var currentFolderId = GetIdFromPath(path, out var history);
            int selectedItemIndex = 0;
            while (true)
            {
                var items = DriveServiceWrapper.Instance.GetItems(currentFolderId, out var currentFolder).ToList();
                Console.Clear();
                Console.WriteLine("Use ↑ and ↓ to navigate. Use → to expand. Use ← to go back. Use [ENTER] to return the ID.");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(string.Join('/', [.. history.Reverse().Select(item => item.Name), currentFolder.Name]));
                Console.ResetColor();
                for (int i = 0; i < items.Count; i++)
                {
                    string indicator = (i == selectedItemIndex) ? ">" : " ";
                    Console.WriteLine($"{indicator} {items[i].Name}");
                }
                var key = Console.ReadKey(intercept: true).Key;
                if (key == ConsoleKey.Enter)
                {
                    Console.Clear();
                    var selectedItem = items[selectedItemIndex];
                    return selectedItem.Id;
                }
                if (key == ConsoleKey.UpArrow)
                {
                    selectedItemIndex = Math.Max(0, selectedItemIndex - 1);
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    selectedItemIndex = Math.Min(items.Count - 1, selectedItemIndex + 1);
                }
                if (key == ConsoleKey.RightArrow)
                {
                    var selectedFolder = items[selectedItemIndex];
                    if (selectedFolder.MimeType == DriveServiceWrapper.folderMimeType)
                    {
                        history.Push(currentFolder);
                        currentFolderId = selectedFolder.Id;
                        selectedItemIndex = 0;
                    }
                }
                if (key == ConsoleKey.LeftArrow)
                {
                    if (history.Count > 0) currentFolderId = history.Pop().Id;
                    else currentFolderId = "root";
                    selectedItemIndex = 0;
                }
            }
        }

        private static string GetIdFromPath(string path, out Stack<Google.Apis.Drive.v3.Data.File> history)
        {
            history = new();
            if (string.IsNullOrEmpty(path) || path == "/" || path == "My Drive") return "root";
            var parts = path.Split('/').Where(p => !string.IsNullOrEmpty(p)).ToList();
            string currentId = "root";
            foreach (var part in parts)
            {
                var items = DriveServiceWrapper.Instance.GetItems(currentId, out var folder);
                var nextItem = items.First(x => x.Name.Equals(part, StringComparison.OrdinalIgnoreCase));
                history.Push(folder);
                currentId = nextItem.Id;
            }
            return currentId;
        }
    }
}

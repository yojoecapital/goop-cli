using System;
using GoogleDrivePushCli.Models;
using GoogleDrivePushCli.Services;

namespace GoogleDrivePushCli.Utilities;

public static class RemotePathResolver
{
    public static RemoteItem ResolveItem(string path, bool isInteractive, string prompt, string selectThisText)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        if (!isInteractive) return DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        return NavigationHelper.Navigate(path, new()
        {
            prompt = prompt,
            selectThisText = selectThisText
        })?.Peek();
    }

    public static RemoteFolder ResolveFolder(string path, bool isInteractive, string prompt, string selectThisText)
    {
        if (string.IsNullOrEmpty(path))
        {
            isInteractive = true;
            path = "/";
        }
        RemoteItem remoteItem;
        if (isInteractive)
        {
            remoteItem = NavigationHelper.Navigate(path, new()
            {
                prompt = prompt,
                selectThisText = selectThisText,
                onlyDisplayFolders = true
            })?.Peek();
            if (remoteItem == null) return null;
        }
        else remoteItem = DataAccessService.Instance.GetRemoteItemsFromPath(path).Peek();
        if (remoteItem is not RemoteFolder remoteFolder)
        {
            throw new Exception($"Remote item '{remoteItem.Name}' ({remoteItem.Id}) is not a folder");
        }
        return remoteFolder;
    }
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using GoogleDrivePushCli.Models;

namespace GoogleDrivePushCli.Utilities;

public static class LinkFileHelper
{
    private static readonly string windowsExtension = ".url";
    private static readonly string osxExtension = ".webloc";
    private static readonly string linuxExtension = ".desktop";
    private static readonly string googleNativeMimeType = "application/vnd.google-apps.";

    private static string GetLinkFileTemplatePath()
    {
        if (!Directory.Exists(Defaults.configurationPath)) return null;
        var matchingFiles = Directory.GetFiles(Defaults.configurationPath, Defaults.linkTempalteFilePattern);
        if (matchingFiles.Length == 0) return null;
        Array.Sort(matchingFiles, StringComparer.Ordinal);
        return matchingFiles[0];
    }

    public static string GetLinkFileExtension()
    {
        var linkFileTemplatePath = GetLinkFileTemplatePath();
        if (linkFileTemplatePath != null) return Path.GetExtension(linkFileTemplatePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return windowsExtension;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return osxExtension;
        return linuxExtension;
    }

    public static bool IsGoogleDriveNativeFile(string mimeType)
    {
        if (string.IsNullOrEmpty(mimeType)) return false;
        return mimeType.StartsWith(googleNativeMimeType, StringComparison.Ordinal) && mimeType != RemoteFolder.MimeType;
    }

    public static void CreateLinkFile(string name, string url, string filePath)
    {
        var linkFileTemplatePath = GetLinkFileTemplatePath();
        if (linkFileTemplatePath == null)
        {
            CreateDefaultLinkFile(name, url, filePath);
            return;
        }
        var content = File.ReadAllText(linkFileTemplatePath).Replace("%NAME%", name).Replace("%URL%", url);
        File.WriteAllText(filePath, content);
        FileManagementHelpers.CopyUnixFileMode(linkFileTemplatePath, filePath);
    }

    private static void CreateDefaultLinkFile(string name, string url, string filePath)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.WriteAllText(filePath, $"[InternetShortcut]\r\nURL={url}\r\n");
            return;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            File.WriteAllText(filePath, $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
  <dict>
    <key>URL</key>
    <string>{url}</string>
  </dict>
</plist>
");
            return;
        }
        File.WriteAllText(filePath, $@"[Desktop Entry]
Encoding=UTF-8
Type=Link
Name={name}
URL={url}
");
        FileManagementHelpers.MakeExecutable(filePath);
    }
}

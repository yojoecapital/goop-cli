# Google Drive Push CLI

The Google Drive Push CLI is a simple tool for syncing files between a local directory and Google Drive.

## Setup

This program requires the **Google Drive API** to be enabled and to have the `credentials.json` saved in the configuration directory for `goop`. These are high level instructions on how you can set that up.

1. go to the [Google Cloud Console](https://console.cloud.google.com/)
2. create a new project
3. enable the Google Drive API for your project
   - navigate to **APIs & Services > Library**
   - search for "Google Drive API" and enable it
4. create OAuth 2.0 credentials
   - go to **APIs & Services > Credentials**
   - click **Create Credentials > OAuth Client ID**
   - choose **Desktop App** as the application type
   - download the `credentials.json` file and save it to the configuration directory
     - Linux: `/home/<user>/.config/goop`
     - Windows: `C:\Users\<user>\AppData\Roaming\goop`

## Installation

You can execute the following command to install or update `goop`.

```bash
curl -L -o /tmp/goop https://github.com/yojoecapital/goop-cli/releases/latest/download/goop && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/
```

## Usage

```bash
goop --help
```

### Initialize

Use `initialize` or `init` to associate a Google Drive folder with the current local directory. Optionally, include `--depth <depth>` to specify the maximum folder depth to sync. After initializing you can use `pull` to download the remote files onto your system.

### Push

Use `push` to upload local changes to the linked Google Drive folder. Passing `--yes` will commit the changes to Google Drive. Otherwise only the potential changes will be printed to the console.

### Pull

Use `pull` to download remote changes from the linked Google Drive folder. Passing `--yes` will commit the changes to your local directory. Otherwise only the potential changes will be printed to the console.

## Configuring

- a cache is stored under `~/.conflig/goop-cli/cache.db`. You can delete this file any time to clear it
- there are additional configuration options in `~/.conflig/goop-cli/config.json`

```json
{
  "cache": {
    "ttl": 300000, // the cache's time to live in milliseconds
    "enabled": true 
  },
  "auto_ignore_list": [ // ignore these files when syncing folders
    ".goop",
    ".goopignore"
  ],
  "default_depth": 3, // the default depth initialized folders use
  "max_depth": 3, // the max depth that can synced
  "shortcut_template": null 
}
```

- the `shortcut_template` option doesn't do anything yet. I plan on using it to define URL shortcut file templates to open files with Google native MIME types like `application/vnd.google-apps.*`

## Building

To compile the project and run it as a self-contained executable:
```bash
cd GoogleDrivePushCli
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

This will generate a single executable file. Replace `linux-x64` with whatever OS your using.

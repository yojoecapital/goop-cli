# Google Drive Push CLI

The **Google Drive Push CLI** is a tool for syncing files between a local directory and Google Drive. It supports pushing and pulling files, managing remote items, and displaying differences between local and remote files.

## Setup

Before you can use the tool, you'll need to enable the **Google Drive API** and set up OAuth 2.0 credentials. Follow these steps to set it up:

1. Go to the [Google Cloud Console](https://console.cloud.google.com/).
2. Create a new project.
3. Enable the **Google Drive API** for your project:
   - Navigate to **APIs & Services > Library**.
   - Search for "Google Drive API" and enable it.
4. Create OAuth 2.0 credentials:
   - Go to **APIs & Services > Credentials**.
   - Click **Create Credentials > OAuth Client ID**.
   - Choose **Desktop App** as the application type.
   - Download the `credentials.json` file and save it to the configuration directory:
     - **Linux**: `/home/<user>/.config/goop`
     - **Windows**: `C:\Users\<user>\AppData\Roaming\goop`

## Installation

To install or update the Google Drive Push CLI, run:

```bash
curl -L -o /tmp/goop https://github.com/yojoecapital/goop-cli/releases/latest/download/goop && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/
```

## Usage

Run the following command to see all available options and commands:

```bash
goop --help
```

### Commands

- `initialize <remote-path>` (or `init`): set up a new sync folder by creating a `.goop` file in the current directory
  - use `--depth <depth>` to set the maximum folder depth to sync
  - if the `<remote-path>` argument is omitted,  an interactive prompt will allow users to traverse their Google Drive directories in the terminal and select the folder to sync

- `push`: upload local changes to the associated Google Drive folder
  - use `--operations [c|u|d]` (or `-x`) to specify which operations should be pushed. The `c` stands for create, `u` for update, and `d` for delete. The default value for this is `cud` for all the operations
  - use `--ignore <glob-pattern>` (or `-i`) to ignore [additional glob patterns](#ignoring-glob-patterns) from being processed

- `pull`: download remote changes from Google Drive to the local directory
  - the same arguments in `push` are present in `pull`

- `diff`: display the differences between the last modified times of local and remote files

#### Remote item management

You can manage remote items with the `remote` command. If you omit the `<path>` argument, an interactive prompt will be used instead.

- `remote info <path>` (or `remote information`): get information for a remote item
- `remote list <path>` (or `remote ls`): list items in a remote folder (default is `/`)
- `remote mkdir <path>`: create a new folder in an existing remote folder
- `remote move <path>` (or `remote mv`): move or reparent an item
- `remote trash <path>`: move an item to the trash
- `remote download <path>`: download a remote item

### Configuration

The program stores a cache under `~/.config/goop-cli/cache.db`, which can be deleted to clear the cache. Additionally, there are configuration options in `~/.config/goop-cli/config.json`.

Here is an example of the configuration file:

```json
{
  "cache": {
    "ttl": 300000, // Cache TTL in milliseconds
    "enabled": true 
  },
  "auth": {
    "max_token_retries": 3, // The max number of times to retry token refreshes
    "retry_delay": 1000 // The delay in milliseconds between token refreshes
  },
  "auto_ignore_list": [ // Files to ignore during sync
    ".goop",
    ".goopignore"
  ],
  "default_depth": 3, // Default sync folder depth
  "max_depth": 3, // Maximum sync folder depth
}
```

#### Ignoring glob patterns

Create a `.goopignore` file inside a sync folder and use it specify a list of glob patterns that should be ignored.

```
secret-file.txt
secret-items/**
```

## Building

To compile and build the project as a self-contained executable:

```bash
cd GoogleDrivePushCli
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

Replace `linux-x64` with the appropriate target platform (e.g. `win-x64`).
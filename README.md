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
     - **Linux**: `$XDG_CONFIG_HOME/goop-cli`, or `~/.config/goop-cli`
     - **Windows**: `%APPDATA%\goop-cli`

Run `goop config --path` to print the exact configuration file location on your machine. Set `GOOP_CONFIG_HOME` to override the configuration directory entirely.

## Installation

Releases are published for both **x86-64** and **ARM64** Linux. Pick the asset that matches your machine:

```bash
# x86-64
curl -L -o /tmp/goop https://github.com/yojoecapital/goop-cli/releases/latest/download/goop-linux-x64 && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/goop

# ARM64 (Raspberry Pi, Ampere, ARM servers)
curl -L -o /tmp/goop https://github.com/yojoecapital/goop-cli/releases/latest/download/goop-linux-arm64 && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/goop
```

To detect your architecture automatically:

```bash
case "$(uname -m)" in
  x86_64)  asset=goop-linux-x64 ;;
  aarch64) asset=goop-linux-arm64 ;;
  *) echo "unsupported architecture: $(uname -m)"; exit 1 ;;
esac
curl -L -o /tmp/goop "https://github.com/yojoecapital/goop-cli/releases/latest/download/$asset" && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/goop
```

## Usage

Run the following command to see all available options and commands:

```bash
goop --help
```

### Commands

- `initialize <remote-path>` (or `init`): set up a new sync folder by creating a `.goop` file in the current directory
  - use `--depth <depth>` to set the maximum folder depth to sync
  - if the `<remote-path>` argument is omitted, an interactive prompt will allow users to traverse their Google Drive directories in the terminal and select the folder to sync

- `status` (or `st`): show the sync folder's local directory, remote folder, depth, cache state, and ignore file

- `push`: upload local changes to the associated Google Drive folder
  - use `--operations [c|u|d]` (or `-x`) to specify which operations should be pushed. The `c` stands for create, `u` for update, and `d` for delete. The default value for this is `cud` for all the operations
  - use `--ignore <glob-pattern>` (or `-i`) to ignore [additional glob patterns](#ignoring-glob-patterns) from being processed
  - use `--yes` (or `-y`) to skip the confirmation prompt

- `pull`: download remote changes from Google Drive to the local directory
  - the same arguments in `push` are present in `pull`

- `diff`: display the differences between the last modified times of local and remote files

- `config`: print the current configuration
  - use `--path` to print the configuration file's location
  - use `--edit` (or `-e`) to open it in `$VISUAL`/`$EDITOR`
  - use `--reset` to restore the defaults

- `completions <bash|zsh|fish>`: generate a shell completion script

- `version`: print the version

- `clear-cache`: delete the local cache database

#### Remote item management

You can manage remote items with the `remote` command. If you omit the `<path>` argument, an interactive prompt will be used instead.

- `remote info <path>` (or `remote information`): get information for a remote item
- `remote list <path>` (or `remote ls`): list items in a remote folder (default is `/`)
- `remote mkdir <path>`: create a new folder in an existing remote folder
- `remote move <path>` (or `remote mv`, `remote rename`): move and/or rename an item
  - use `--into <remote-path>` to reparent the item
  - use `--name <name>` (or `-n`) to rename it; passing only `--name` renames in place
- `remote copy <path>` (or `remote cp`): copy an item into a remote folder
  - use `--into <remote-path>` to choose the destination and `--name` to name the copy
  - folders are copied recursively up to `--depth`
- `remote trash <path>` (or `remote rm`): move an item to the trash
  - use `--list` to list the items currently in the trash
  - use `--empty` to permanently delete everything in the trash
  - use `--yes` (or `-y`) to skip the confirmation prompt
- `remote restore <name>` (or `remote untrash`): restore an item from the trash
- `remote download <path>`: download a remote item
- `remote upload <local-path>` (or `remote up`): upload a local file or folder into a remote folder
  - use `--into <remote-path>` to choose the destination folder

### Shell completions

```bash
# bash
goop completions bash | sudo tee /etc/bash_completion.d/goop > /dev/null

# zsh
goop completions zsh > "${fpath[1]}/_goop"

# fish
goop completions fish > ~/.config/fish/completions/goop.fish
```

### Configuration

The program stores a cache under the configuration directory as `cache.db`, which can be deleted (or cleared with `goop clear-cache`) to reset the cache. Configuration options live alongside it in `config.json`.

Here is an example of the configuration file:

```json
{
  "cache": {
    "ttl": 300000,
    "enabled": true
  },
  "auth": {
    "max_token_retries": 3,
    "retry_delay": 1000
  },
  "auto_ignore_list": [
    ".goop",
    ".goopignore"
  ],
  "default_depth": 3,
  "max_depth": 3
}
```

| Key | Meaning |
| --- | --- |
| `cache.ttl` | Cache lifetime in milliseconds |
| `cache.enabled` | Whether remote metadata is cached locally |
| `auth.max_token_retries` | Maximum number of token refresh attempts |
| `auth.retry_delay` | Delay in milliseconds between token refresh attempts |
| `auto_ignore_list` | Patterns always ignored during sync |
| `default_depth` | Default sync folder depth |
| `max_depth` | Maximum sync folder depth |

Comments and trailing commas are tolerated when the configuration file is read, so annotating it by hand is safe.

#### Ignoring glob patterns

Create a `.goopignore` file inside a sync folder and use it to specify a list of glob patterns that should be ignored. Blank lines and lines starting with `#` are skipped, and a leading `!` negates a pattern.

```
# secrets
secret-file.txt
secret-items/**
```

## Building

Build the project as a self-contained, single-file executable. The repository uses a git submodule, so clone with `--recurse-submodules` (or run `git submodule update --init --recursive` in an existing clone).

```bash
cd GoogleDrivePushCli
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
dotnet publish -c Release -r linux-arm64 --self-contained true /p:PublishSingleFile=true
```

Supported runtime identifiers are `linux-x64`, `linux-arm64`, `win-x64`, `win-arm64`, `osx-x64`, and `osx-arm64`. Omitting `-r` builds for the host machine's own architecture.

Release binaries for `linux-x64` and `linux-arm64` are built and attached to a GitHub release automatically by `.github/workflows/release.yml` when a tag is pushed.

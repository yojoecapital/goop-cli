## Major version release

- folders synced with previous versions of `goop` will not work with `3.0.0`
- it'd probably be easiest to sync your folder with your current version of `goop`, delete the `.goop` file in the sync folder, then re-sync with the newest version

## Features

- `goop` now uses [Spectre.Console](https://spectreconsole.net/) for nice interactive prompts
- you can also interact directly with Google Drive without a sync folder by using the `remote` subcommands
- `goop` now uses a global cache to map your Google Drive structure. This is stored at `~/.config/goop-cli/cache.db`
- you can use the `diff` command to view the differences in last modified times between local and remote items for a sync folder
- to get started, just run `goop init --working-dir <path-to-sync-folder>`
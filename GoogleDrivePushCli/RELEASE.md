## Breaking Changes
- folders synced with previous versions of `goop` are **not compatible** with `3.0.0`
- to fix this, sync your folder with your current version of `goop`, delete the `.goop` file in the sync folder, and then re-sync using the latest version

## New Features
- **Interactive prompts**: `goop` now uses [Spectre.Console](https://spectreconsole.net/) for improved, interactive terminal prompts
- **Remote item management**: you can interact with Google Drive directly using the `remote` subcommands, no sync folder required
- **Global cache**: a global cache (`~/.config/goop-cli/cache.db`) is now used to map your Google Drive structure for faster operations
- **`diff` command**: view differences in last modified times between local and remote items for any sync folder
- **Ignore file**: use a `.goopignore` file to specify a list of glob patterns that `goop` should ignore when process items in a sync folder
- **Operation filtering**: 
  - you can filter `push` and `pull` operations using the `--operations [c|u|d]` flag
  - you can also specify additional ignored glob patterns using `--ignore <glob-pattern>`


## Getting started

A bit of [setup](https://github.com/yojoecapital/goop-cli?tab=readme-ov-file#setup) is required before `goop` can interact with Google Drive's API. Once that's done, you can use the tool with ease.

```bash
# enter a directory you'd like to sync with Google Drive
cd my-sync-folder

# initialize the sync folder
goop init 

# pull the remote items from Google Drive
goop pull

# push your changes to Google Drive
goop push
```


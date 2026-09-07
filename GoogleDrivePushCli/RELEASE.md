## 3.2.0

This release adds ARM64 binaries, fills out the remote command set, and fixes several bugs that could cause data loss.

## Installation

Releases now ship for both **x86-64** and **ARM64** Linux.

```bash
case "$(uname -m)" in
  x86_64)  asset=goop-linux-x64 ;;
  aarch64) asset=goop-linux-arm64 ;;
  *) echo "unsupported architecture: $(uname -m)"; exit 1 ;;
esac
curl -L -o /tmp/goop "https://github.com/yojoecapital/goop-cli/releases/latest/download/$asset" && chmod 755 /tmp/goop && sudo mv /tmp/goop /usr/local/bin/goop
```

## Fixes

- **Folders containing Google Docs, Sheets, Slides, or Forms could not be listed at all.** Drive omits `size` for these files, and reading it unconditionally threw, surfacing as `Failed to fetch items for folder with ID (...)`. This made `push`, `pull`, `diff`, and `remote ls` fail on any folder holding a native Google file.
- **Folders with more than 100 items were silently truncated.** Drive paginates listings and the extra pages were never requested, so `pull` could propose deleting local files that still existed remotely, and `push` could re-upload duplicates. Listings are now fully paginated.
- **`remote move` did not move anything.** The new parent was added without removing the old one, leaving the item in its original folder.
- **Configuration could be written to the current working directory.** When the user configuration directory did not already exist, the resolved path degraded to a relative one, placing `config.json`, the cache, and OAuth credentials wherever `goop` happened to be run. The first run on a clean machine could also fail outright. Set `GOOP_CONFIG_HOME` to choose the directory explicitly, or run `goop config --path` to see where it resolves.
- **Ignore patterns were skipped for newly created folders.** The first `push` of a new folder uploaded every file in it, including ones matched by `.goopignore`.
- **The configuration example in the README could not be parsed.** Comments and trailing commas are now accepted when reading `config.json`.
- `remote trash` now asks for confirmation before trashing; use `--yes` to skip it.
- Ignore patterns now match nested paths on Windows, and `.goopignore` supports `#` comments.
- A single failed transfer no longer aborts the rest of a `push` or `pull`; failures are reported at the end.
- Progress reporting no longer produces `NaN` for empty files, and the local cache is now versioned so upgrades rebuild it instead of failing.
- The sync folder search no longer stops after `max_depth` parent directories, so `push` works from deeper subdirectories.
- `remote info` prints the modified time as a date rather than a raw timestamp.

## New Features

- `goop status` shows the sync folder's local directory, remote folder, depth, cache state, and ignore file.
- `goop config` prints the configuration, with `--path`, `--edit`, and `--reset`.
- `goop completions bash|zsh|fish` generates a shell completion script.
- `goop version` is now a real subcommand.
- `goop remote upload` uploads a local file or folder into a remote folder.
- `goop remote copy` copies a remote item, recursing into folders.
- `goop remote move --name` renames an item, in place or while moving it.
- `goop remote restore` recovers an item from the trash.

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

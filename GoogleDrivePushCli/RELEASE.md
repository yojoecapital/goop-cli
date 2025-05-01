## Breaking Changes

There is now retry logic if token refresh fails. Configuring retry logic can be done in `~/.config/goop-cli/config.json` with:

```json
{
  "auth": {
    "max_token_retries": 3, // The max number of times to retry token refreshes
    "retry_delay": 1000 // The delay in milliseconds between token refreshes
  }
}
```


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


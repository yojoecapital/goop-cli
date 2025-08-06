## 

## New Features

Google Drive native files (i.e., Google Docs, Slides, Sheets, Forms, etc.) will now sync with a **link file**.

### Template Support (Optional)

You can provide a custom link file template by placing a file matching the pattern `link-template.*` in the configuration directory. The generated link files will match the template's extension as well as file permissions. Supported placeholders in the template:

- `%NAME%` → Replaced with the document name
- `%URL%` → Replaced with the Google Drive URL of the file

### Fallback Behavior

 If no template is found, the system automatically generates a default link file format based on the OS:

- **Windows `.url`**: Uses `[InternetShortcut]` syntax
- **macOS `.webloc`**: Generates an XML `.plist`
- **Linux `.desktop`**: Creates a `.desktop` file with a URL and marks it executable


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


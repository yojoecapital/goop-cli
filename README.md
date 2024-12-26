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

## Usage

```bash
goop --help # ❓
```

### Initialize

Use `initialize <folderId>` or `init <folderId>` to associate a Google Drive folder with the current local directory. Optionally, include `--depth <depth>` to specify the maximum folder depth to sync. After initializing you can use `pull` to download the remove files onto your system.

### Fetch

Use `fetch` to update the Google Drive cache in the metadata file. Both the push and pull commands determine what to push and pull based on this cache.

### Push

Upload local changes to the linked Google Drive folder. Passing `--yes` will commit the changes to Google Drive. Otherwise only the potential changes will be printed to the console.

### Pull

Download remote changes from the linked Google Drive folder. Passing `--yes` will commit the changes to your local directory. Otherwise only the potential changes will be printed to the console.

### Ignore

Use `ignore <path>` to add an item with the path `<path>` to the ignored list. Files under the ignored list will not be pulled or pushed. Passing `--remove` will remove the file from the ignored list.

## Building

To compile the project and run it as a self-contained executable:
```bash
dotnet publish -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true
```

This will generate a single executable file. Replace `linux-x64` with whatever OS your using.

#!/bin/bash

# Project Requirements:
# The script should be executed from within the project directory, not from outside.
# The project directory must contain:
#  - A `.version` file, which should define the `VERSION` variable and a `FILES` variable with comma-separated paths to files that should be uploaded.
#  - `.csproj` files (C# project files) to update version information in the version tag.
#  - A `Program.cs` file that contains a line to update a version string (e.g., `private static readonly string version = "X.X.X";`).
#  - Git initialized in the project directory (repository should be initialized with `git init`).
#  - A valid `RELEASE.md` file (optional, for release notes). If this file is present, the script will use it for creating release notes.
# The project should be a .NET-based project (as indicated by the `dotnet publish` commands).
# GitHub CLI (`gh`) must be installed to create GitHub releases.
# The script expects the project to be self-contained, building for both `linux-x64` and `win-x64` runtimes.

set -e

BASE_PATH=$(dirname "$(realpath "$0")")

if [ "$(pwd)" != "$BASE_PATH" ]; then
    echo "Please run this command inside the project directory."
    exit 1
fi

source .version

echo "Setting version to $VERSION..."
find . -name "*.csproj" -exec sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" {} +
sed -i "s|public static readonly string version = \".*\";|public static readonly string version = \"${VERSION}\";|" Program.cs

echo "Building and publishing project..."
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r win-x64

IFS=',' read -r -a FILE_ARRAY <<< "$FILES"
FILES_TO_UPLOAD=$(IFS=' '; echo "${FILE_ARRAY[*]}")

echo "Files to be uploaded:"
for file in "${FILE_ARRAY[@]}"; do
    echo "$file"
done

if [ "$1" != "YES" ]; then
    echo "Please pass YES to the command to publish $VERSION."
    exit 0
fi

# Make sure the project is up to date
git fetch
git pull

# Check for existing tag/release
echo "Checking for existing tag/release..."
if git rev-parse "$VERSION" >/dev/null 2>&1; then
  echo "Tag $VERSION exists. Deleting existing tag and release..."
  git tag -d "$VERSION"
  git push origin --delete "$VERSION"
  gh release delete "$VERSION" --yes
fi

echo "Committing version update and creating new tag..."
git tag "$VERSION"
git add .
if git diff-index --quiet HEAD --; then
    echo "No changes to commit."
else
    git commit -m "Release version $VERSION"
fi
git push origin --tags

# Create new GitHub release
echo "Publishing GitHub release..."
if [ -f "RELEASE.md" ]; then
  gh release create "$VERSION" $FILES_TO_UPLOAD --notes-file "RELEASE.md" --title "$VERSION"
else
  gh release create "$VERSION" $FILES_TO_UPLOAD --title "$VERSION"
fi

echo "Release $VERSION deployed successfully!"

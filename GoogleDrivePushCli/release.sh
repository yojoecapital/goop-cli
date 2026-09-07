#!/bin/bash

set -euo pipefail

BASE_PATH=$(dirname "$(realpath "$0")")

if [ "$(pwd)" != "$BASE_PATH" ]; then
    echo "Please run this command inside the project directory."
    exit 1
fi

source .version

IFS=',' read -r -a RUNTIME_ARRAY <<< "$RUNTIMES"

echo "Setting version to $VERSION..."
find . -name "*.csproj" -exec sed -i "s|<Version>.*</Version>|<Version>${VERSION}</Version>|" {} +
sed -i "s|public static readonly string version = \".*\";|public static readonly string version = \"${VERSION}\";|" Program.cs

STAGING_DIR="$BASE_PATH/dist"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"

for runtime in "${RUNTIME_ARRAY[@]}"; do
    echo "Building and publishing for $runtime..."
    dotnet publish -c Release -r "$runtime" --self-contained true /p:PublishSingleFile=true
    published="bin/Release/net8.0/${runtime}/publish/goop"
    if [ ! -f "$published" ]; then
        echo "Expected published binary at '$published' was not produced."
        exit 1
    fi
    cp "$published" "$STAGING_DIR/goop-${runtime}"
    chmod 755 "$STAGING_DIR/goop-${runtime}"
done

FILES_TO_UPLOAD=()
for runtime in "${RUNTIME_ARRAY[@]}"; do
    FILES_TO_UPLOAD+=("$STAGING_DIR/goop-${runtime}")
done

echo "Files to be uploaded:"
for file in "${FILES_TO_UPLOAD[@]}"; do
    echo "  $file"
done

if [ "${1:-}" != "YES" ]; then
    echo "Please pass YES to the command to publish $VERSION."
    exit 0
fi

git fetch
git pull

echo "Checking for existing tag/release..."
if git rev-parse "$VERSION" >/dev/null 2>&1; then
  echo "Tag $VERSION exists. Deleting existing tag and release..."
  git tag -d "$VERSION"
  git push origin --delete "$VERSION" || true
  gh release delete "$VERSION" --yes || true
fi

echo "Committing version update and creating new tag..."
git add .
if git diff-index --quiet HEAD --; then
    echo "No changes to commit."
else
    git commit -m "Release version $VERSION"
fi
git tag "$VERSION"
git push origin HEAD
git push origin --tags

echo "Publishing GitHub release..."
if [ -f "RELEASE.md" ]; then
  gh release create "$VERSION" "${FILES_TO_UPLOAD[@]}" --notes-file "RELEASE.md" --title "$VERSION"
else
  gh release create "$VERSION" "${FILES_TO_UPLOAD[@]}" --title "$VERSION"
fi

echo "Release $VERSION deployed successfully!"

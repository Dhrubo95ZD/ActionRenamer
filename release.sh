#!/usr/bin/env bash
# Publishes a new version to the custom Dalamud repo.
# Usage: ./release.sh 1.2.0
# Builds the icon pack and plugin, updates repo.json, commits, pushes and creates the GitHub release.
set -euo pipefail
cd "$(dirname "$0")"
v="${1:?usage: ./release.sh <version, e.g. 1.2.0>}"

sed -i "s#<Version>.*</Version>#<Version>$v</Version>#" ActionRenamer.csproj
dotnet run -c Release --project tools/IconPack -- icons/replacements ActionRenamerIcons.pmp
dotnet build -c Release ActionRenamer.csproj

now=$(date +%s)
sed -i -E "s#\"AssemblyVersion\": \"[^\"]*\"#\"AssemblyVersion\": \"$v.0\"#; s#\"LastUpdate\": [0-9]+#\"LastUpdate\": $now#" repo.json

git add -A
git commit -m "chore: release $v"
git push
gh release create "v$v" bin/Release/ActionRenamer/latest.zip --title "v$v" --notes "Action Renamer $v"
echo "Released $v. Dalamud picks it up on the next plugin update check."

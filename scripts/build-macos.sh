#!/usr/bin/env bash
set -euo pipefail

case "$(uname -m)" in
  arm64) arch="arm64" ;;
  x86_64) arch="x64" ;;
  *) echo "unsupported macOS architecture: $(uname -m)" >&2; exit 2 ;;
esac

cd "$(dirname "$0")/.."
publish_dir="$(mktemp -d)"
trap 'rm -R "$publish_dir"' EXIT

dotnet restore src/MultiDiskImager.App/MultiDiskImager.App.csproj --locked-mode
dotnet publish src/MultiDiskImager.App/MultiDiskImager.App.csproj \
  --configuration Release --runtime "osx-$arch" --self-contained true --no-restore \
  --output "$publish_dir"

bash scripts/package-macos.sh "$publish_dir" 0.1.0 "$arch" artifacts

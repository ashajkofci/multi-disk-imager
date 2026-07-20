#!/usr/bin/env bash
set -euo pipefail

if [[ $# -ne 4 ]]; then
  echo "usage: $0 PUBLISH_DIR VERSION ARCH OUTPUT_DIR" >&2
  exit 2
fi

publish_dir="$1"
version="$2"
arch="$3"
output_dir="$4"
app_dir="$output_dir/bNovate Multi Disk Imager.app"
executable="$publish_dir/MultiDiskImager"

case "$arch" in
  x64) expected_arch="x86_64" ;;
  arm64) expected_arch="arm64" ;;
  *) echo "unsupported macOS architecture: $arch" >&2; exit 2 ;;
esac

if [[ ! -x "$executable" ]]; then
  echo "published executable is missing or not executable: $executable" >&2
  exit 1
fi

if ! lipo -archs "$executable" | tr ' ' '\n' | grep -Fxq "$expected_arch"; then
  echo "published executable does not contain the expected $expected_arch architecture" >&2
  lipo -archs "$executable" >&2
  exit 1
fi

DOTNET_BUNDLE_EXTRACT_BASE_DIR="$output_dir/.bundle-extract" "$executable" --version >/dev/null
rm -R "$output_dir/.bundle-extract" 2>/dev/null || true

mkdir -p "$app_dir/Contents/MacOS"
mkdir -p "$app_dir/Contents/Resources"
sed "s/__VERSION__/$version/g" packaging/macos/Info.plist.template > "$app_dir/Contents/Info.plist"
cp "$executable" "$app_dir/Contents/MacOS/MultiDiskImager"

iconset="$output_dir/AppIcon.iconset"
mkdir -p "$iconset"
for icon in \
  'icon_16x16.png:16' 'icon_16x16@2x.png:32' \
  'icon_32x32.png:32' 'icon_32x32@2x.png:64' \
  'icon_128x128.png:128' 'icon_128x128@2x.png:256' \
  'icon_256x256.png:256' 'icon_256x256@2x.png:512' \
  'icon_512x512.png:512' 'icon_512x512@2x.png:1024'; do
  name="${icon%%:*}"
  size="${icon##*:}"
  sips -z "$size" "$size" src/MultiDiskImager.App/Assets/AppIcon.png --out "$iconset/$name" >/dev/null
done
iconutil -c icns "$iconset" -o "$app_dir/Contents/Resources/AppIcon.icns"
rm -R "$iconset"
chmod 755 "$app_dir/Contents/MacOS/MultiDiskImager"
plutil -lint "$app_dir/Contents/Info.plist" >/dev/null

# Apple Silicon requires code-signing metadata even for builds distributed
# without a Developer ID. An ad-hoc signature remains user-overridable in
# Privacy & Security while avoiding a structurally invalid app bundle.
codesign --force --sign - "$app_dir/Contents/MacOS/MultiDiskImager"
codesign --force --sign - "$app_dir"
codesign --verify --deep --strict --verbose=2 "$app_dir"
DOTNET_BUNDLE_EXTRACT_BASE_DIR="$output_dir/.bundle-extract" "$app_dir/Contents/MacOS/MultiDiskImager" --version >/dev/null
rm -R "$output_dir/.bundle-extract" 2>/dev/null || true

archive="$output_dir/bnovate-multi-disk-imager-$version-macos-$arch.zip"
ditto -c -k --sequesterRsrc --keepParent "$app_dir" "$archive"
rm -R "$app_dir"
echo "$archive"

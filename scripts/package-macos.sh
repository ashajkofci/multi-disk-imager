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

mkdir -p "$app_dir/Contents/MacOS"
mkdir -p "$app_dir/Contents/Resources"
sed "s/__VERSION__/$version/g" packaging/macos/Info.plist.template > "$app_dir/Contents/Info.plist"
cp "$publish_dir/MultiDiskImager" "$app_dir/Contents/MacOS/MultiDiskImager"

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

archive="$output_dir/bnovate-multi-disk-imager-$version-macos-$arch.zip"
ditto -c -k --sequesterRsrc --keepParent "$app_dir" "$archive"
rm -R "$app_dir"
echo "$archive"

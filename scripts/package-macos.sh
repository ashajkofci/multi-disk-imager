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
app_dir="$output_dir/Multi Disk Imager.app"

mkdir -p "$app_dir/Contents/MacOS"
sed "s/__VERSION__/$version/g" packaging/macos/Info.plist.template > "$app_dir/Contents/Info.plist"
cp "$publish_dir/MultiDiskImager" "$app_dir/Contents/MacOS/MultiDiskImager"
chmod 755 "$app_dir/Contents/MacOS/MultiDiskImager"

archive="$output_dir/multi-disk-imager-$version-macos-$arch.zip"
ditto -c -k --sequesterRsrc --keepParent "$app_dir" "$archive"
rm -R "$app_dir"
echo "$archive"

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
stage_dir="$output_dir/bnovate-multi-disk-imager-$version-linux-$arch"
archive="$output_dir/bnovate-multi-disk-imager-$version-linux-$arch.tar.gz"

mkdir -p "$stage_dir"
cp "$publish_dir/MultiDiskImager" "$stage_dir/MultiDiskImager"
chmod 755 "$stage_dir/MultiDiskImager"
cp src/MultiDiskImager.App/Assets/AppIcon.png "$stage_dir/bnovate-multi-disk-imager.png"
cp README.md LICENSE "$stage_dir/"
tar -C "$output_dir" -czf "$archive" "$(basename "$stage_dir")"
rm -R "$stage_dir"
echo "$archive"

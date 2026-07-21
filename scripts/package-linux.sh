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

case "$arch" in
  x64) deb_arch="amd64" ;;
  arm64) deb_arch="arm64" ;;
  *)
    echo "unsupported Debian architecture: $arch (expected x64 or arm64)" >&2
    exit 2
    ;;
esac

executable="$publish_dir/MultiDiskImager"
if [[ ! -f "$executable" ]]; then
  echo "published executable not found: $executable" >&2
  exit 1
fi

mkdir -p "$output_dir"
package_root="$(mktemp -d "${TMPDIR:-/tmp}/bnovate-multi-disk-imager-deb.XXXXXX")"
trap 'rm -R "$package_root"' EXIT

package_name="bnovate-multi-disk-imager"
deb_version="${version/-/\~}"
package_dir="$package_root/$package_name"
package_file="$output_dir/$package_name-$version-linux-$arch.deb"

mkdir -p \
  "$package_dir/DEBIAN" \
  "$package_dir/usr/bin" \
  "$package_dir/usr/lib/$package_name" \
  "$package_dir/usr/share/applications" \
  "$package_dir/usr/share/doc/$package_name" \
  "$package_dir/usr/share/icons/hicolor/1024x1024/apps"

cat > "$package_dir/DEBIAN/control" <<EOF
Package: $package_name
Version: $deb_version
Section: utils
Priority: optional
Architecture: $deb_arch
Maintainer: bNovate Technologies SA <info@bnovate.com>
Depends: libc6, libfontconfig1, libfreetype6, libice6, libsm6, libx11-6, libx11-xcb1, libxcb1, libxext6, mount, policykit-1, util-linux
Homepage: https://www.bnovate.com
Description: Read, write, and verify raw disk images
 A cross-platform desktop utility that can write one raw image to multiple
 removable devices in parallel and verify the result byte for byte.
EOF

cat > "$package_dir/usr/share/applications/$package_name.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=bNovate Multi Disk Imager
Comment=Read, write, and verify raw disk images
Exec=/usr/bin/$package_name %F
Icon=$package_name
Terminal=false
Categories=System;Utility;
Keywords=disk;image;flash;write;verify;
EOF

install -m 755 "$executable" "$package_dir/usr/lib/$package_name/MultiDiskImager"
ln -s "../lib/$package_name/MultiDiskImager" "$package_dir/usr/bin/$package_name"
ln -s "../lib/$package_name/MultiDiskImager" "$package_dir/usr/bin/MultiDiskImager"
install -m 644 src/MultiDiskImager.App/Assets/AppIcon.png \
  "$package_dir/usr/share/icons/hicolor/1024x1024/apps/$package_name.png"
install -m 644 LICENSE "$package_dir/usr/share/doc/$package_name/copyright"

dpkg-deb --build --root-owner-group "$package_dir" "$package_file"
echo "$package_file"

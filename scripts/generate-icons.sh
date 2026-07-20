#!/usr/bin/env bash
set -euo pipefail

php scripts/generate-windows-assets.php
convert src/MultiDiskImager.App/Assets/AppIcon.png \
  -define icon:auto-resize=256,128,64,48,32,24,16 \
  src/MultiDiskImager.App/Assets/AppIcon.ico

echo "Generated application, executable, and MSIX icons."

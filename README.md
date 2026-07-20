# Multi Disk Imager

Multi Disk Imager is a cross-platform desktop utility for reading, writing, and verifying raw disk images. It can write one image to several devices concurrently and preserves a plain, byte-for-byte `.img` format.

Unlike the original dotNet Disk Imager, this application deliberately has no ZIP compression or encryption mode. It never adds a header, container, or proprietary metadata to an image.

## Features

- Read a physical device to a raw `.img` file.
- Write one image to multiple devices in parallel; a failure on one target does not stop healthy targets.
- Verify an image against one or more devices and report the first mismatching byte.
- Optionally verify automatically after a read or write.
- Read only through the last allocated MBR or GPT partition.
- Detect an oversized image, inspect the discarded tail for non-zero data, and require explicit approval before cropping.
- Quick-wipe partition/filesystem metadata at the beginning and end of a device.
- MD5, SHA-1, and SHA-256 image checksums.
- Transfer speed graph, progress, throughput, and remaining-time estimates.
- Device identity revalidation immediately before elevated access.
- GitHub Releases update checks.

The system disk is never offered as a target. Writes and wipes require administrator approval and show the selected model, size, and device identifier before destructive access.

## Downloads

SemVer releases publish unsigned, self-contained builds for:

- Windows 10+ x64: one `.exe` with its runtime and native dependencies embedded.
- macOS 12+ Intel: a zipped `.app`.
- macOS 12+ Apple Silicon: a zipped `.app`.

Because the builds are intentionally unsigned, Windows SmartScreen or macOS Gatekeeper may warn on first launch. On macOS, use **Open** from Finder's context menu if Gatekeeper blocks the app. Administrator approval is requested only when raw-device access begins.

## Command line

```text
MultiDiskImager [image.img] [options]
  -i, -image, --image PATH       Select a raw image file
  -d, -device, --device ID ...  Select platform device IDs
  -r, -read, --read             Read one device to the image
  -w, -write, --write           Write the image to selected devices
  -v, -verify, --verify         Verify, or verify after read/write
  -oa, -onlyallocated, --only-allocated
                                Stop at the last allocated partition
  -s, -start, --start           Start after validation
      --version                 Print the version
  -h, --help                    Show help
```

Legacy compression (`-z`) and encryption (`-e`) arguments fail with a clear error instead of silently changing the image format.

## Build and test

The app targets .NET 8 for its runtime baseline. Avalonia 12's source generators require the .NET 10 SDK to build it.

```bash
dotnet restore MultiDiskImager.sln --locked-mode -m:1
dotnet build MultiDiskImager.sln -c Release --no-restore -m:1
dotnet test MultiDiskImager.sln -c Release --no-build
```

Create a release by pushing an annotated or lightweight SemVer tag such as `v1.2.3`. GitHub Actions validates the tag, builds every platform artifact, creates `SHA256SUMS.txt` and `release-manifest.json`, and attaches them to the GitHub Release.

## Safety

Raw disk imaging can destroy data. Confirm the model, capacity, and platform device ID every time. Keep backups and disconnect devices that are not part of the operation. Canceling or losing power during a write can leave the target unusable until it is rewritten.

## License

MIT. See [LICENSE](LICENSE).

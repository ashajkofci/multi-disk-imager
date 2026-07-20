using System.Buffers.Binary;
using System.Text;

namespace MultiDiskImager.Core;

public static class PartitionTableParser
{
    private const int MinimumSectorSize = 512;

    public static async Task<PartitionLayout> ParseAsync(
        Stream disk,
        long diskSize,
        int sectorSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(disk);
        if (!disk.CanRead || !disk.CanSeek)
        {
            throw new ArgumentException("Disk stream must be readable and seekable.", nameof(disk));
        }

        if (diskSize <= 0 || sectorSize < MinimumSectorSize)
        {
            return new PartitionLayout(PartitionScheme.Raw, Math.Max(0, diskSize), Math.Max(MinimumSectorSize, sectorSize), []);
        }

        var firstSector = new byte[sectorSize];
        disk.Position = 0;
        if (await ReadExactlyAsync(disk, firstSector, cancellationToken).ConfigureAwait(false) < MinimumSectorSize ||
            firstSector[510] != 0x55 || firstSector[511] != 0xAA)
        {
            return new PartitionLayout(PartitionScheme.Raw, diskSize, sectorSize, []);
        }

        var mbrPartitions = ParseMbr(firstSector, diskSize, sectorSize);
        var protectiveGpt = mbrPartitions.Any(partition => partition.Type.Equals("0xEE", StringComparison.OrdinalIgnoreCase));
        if (!protectiveGpt)
        {
            return new PartitionLayout(PartitionScheme.Mbr, diskSize, sectorSize, mbrPartitions);
        }

        var gpt = await TryParseGptAsync(disk, diskSize, sectorSize, cancellationToken).ConfigureAwait(false);
        return gpt ?? new PartitionLayout(PartitionScheme.Mbr, diskSize, sectorSize, mbrPartitions);
    }

    private static IReadOnlyList<PartitionDescriptor> ParseMbr(byte[] sector, long diskSize, int sectorSize)
    {
        var partitions = new List<PartitionDescriptor>(4);
        for (var index = 0; index < 4; index++)
        {
            var offset = 446 + index * 16;
            var type = sector[offset + 4];
            var startLba = BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan(offset + 8, 4));
            var sectorCount = BinaryPrimitives.ReadUInt32LittleEndian(sector.AsSpan(offset + 12, 4));
            if (type == 0 || sectorCount == 0)
            {
                continue;
            }

            var start = checked((long)startLba * sectorSize);
            var length = checked((long)sectorCount * sectorSize);
            if (start >= diskSize)
            {
                continue;
            }

            length = Math.Min(length, diskSize - start);
            partitions.Add(new PartitionDescriptor(index + 1, start, length, $"0x{type:X2}"));
        }

        return partitions;
    }

    private static async Task<PartitionLayout?> TryParseGptAsync(
        Stream disk,
        long diskSize,
        int sectorSize,
        CancellationToken cancellationToken)
    {
        var header = new byte[sectorSize];
        disk.Position = sectorSize;
        if (await ReadExactlyAsync(disk, header, cancellationToken).ConfigureAwait(false) < 92 ||
            !header.AsSpan(0, 8).SequenceEqual("EFI PART"u8))
        {
            return null;
        }

        var entriesLba = BinaryPrimitives.ReadUInt64LittleEndian(header.AsSpan(72, 8));
        var entryCount = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(80, 4));
        var entrySize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(84, 4));
        if (entryCount == 0 || entrySize < 128 || entrySize > 4096 || entryCount > 4096)
        {
            return null;
        }

        var tableLength = checked((long)entryCount * entrySize);
        var tableOffset = checked((long)entriesLba * sectorSize);
        if (tableOffset < 0 || tableOffset >= diskSize || tableLength > diskSize - tableOffset)
        {
            return null;
        }

        var entries = new byte[checked((int)tableLength)];
        disk.Position = tableOffset;
        if (await ReadExactlyAsync(disk, entries, cancellationToken).ConfigureAwait(false) != entries.Length)
        {
            return null;
        }

        return ParseGptEntries(entries, entryCount, entrySize, diskSize, sectorSize);
    }

    private static PartitionLayout ParseGptEntries(byte[] entries, uint entryCount, uint entrySize, long diskSize, int sectorSize)
    {
        var partitions = new List<PartitionDescriptor>();
        for (var index = 0; index < entryCount; index++)
        {
            var entry = entries.AsSpan(checked((int)(index * entrySize)), checked((int)entrySize));
            if (entry[..16].IndexOfAnyExcept((byte)0) < 0)
            {
                continue;
            }

            var firstLba = BinaryPrimitives.ReadUInt64LittleEndian(entry.Slice(32, 8));
            var lastLba = BinaryPrimitives.ReadUInt64LittleEndian(entry.Slice(40, 8));
            if (lastLba < firstLba || firstLba > long.MaxValue / (ulong)sectorSize)
            {
                continue;
            }

            var start = checked((long)firstLba * sectorSize);
            var length = checked((long)(lastLba - firstLba + 1) * sectorSize);
            if (start >= diskSize)
            {
                continue;
            }

            var nameBytes = entry.Slice(56, Math.Min(72, entry.Length - 56));
            var name = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
            var type = new Guid(entry[..16]).ToString("D");
            partitions.Add(new PartitionDescriptor(index + 1, start, Math.Min(length, diskSize - start), type, string.IsNullOrWhiteSpace(name) ? null : name));
        }

        return new PartitionLayout(PartitionScheme.Gpt, diskSize, sectorSize, partitions);
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            total += read;
        }

        return total;
    }
}

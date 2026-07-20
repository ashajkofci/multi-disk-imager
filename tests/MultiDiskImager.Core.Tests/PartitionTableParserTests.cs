using System.Buffers.Binary;
using System.Text;
using MultiDiskImager.Core;

namespace MultiDiskImager.Core.Tests;

public sealed class PartitionTableParserTests
{
    [Fact]
    public async Task ParsesMbrPartitions()
    {
        var disk = new byte[512 * 100];
        disk[510] = 0x55;
        disk[511] = 0xAA;
        disk[450] = 0x0C;
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(454, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(458, 4), 50);

        var layout = await PartitionTableParser.ParseAsync(new MemoryStream(disk), disk.Length, 512);

        Assert.Equal(PartitionScheme.Mbr, layout.Scheme);
        var partition = Assert.Single(layout.Partitions);
        Assert.Equal(4 * 512, partition.StartByte);
        Assert.Equal(50 * 512, partition.Length);
        Assert.Equal(54 * 512, layout.LastAllocatedByte);
    }

    [Fact]
    public async Task ParsesProtectiveMbrAndGpt()
    {
        var disk = new byte[512 * 200];
        disk[510] = 0x55;
        disk[511] = 0xAA;
        disk[450] = 0xEE;
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(454, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(458, 4), 199);
        "EFI PART"u8.CopyTo(disk.AsSpan(512, 8));
        BinaryPrimitives.WriteUInt64LittleEndian(disk.AsSpan(512 + 72, 8), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(512 + 80, 4), 4);
        BinaryPrimitives.WriteUInt32LittleEndian(disk.AsSpan(512 + 84, 4), 128);
        Guid.NewGuid().TryWriteBytes(disk.AsSpan(1024, 16));
        BinaryPrimitives.WriteUInt64LittleEndian(disk.AsSpan(1024 + 32, 8), 40);
        BinaryPrimitives.WriteUInt64LittleEndian(disk.AsSpan(1024 + 40, 8), 99);
        Encoding.Unicode.GetBytes("Data").CopyTo(disk, 1024 + 56);

        var layout = await PartitionTableParser.ParseAsync(new MemoryStream(disk), disk.Length, 512);

        Assert.Equal(PartitionScheme.Gpt, layout.Scheme);
        var partition = Assert.Single(layout.Partitions);
        Assert.Equal("Data", partition.Name);
        Assert.Equal(40 * 512, partition.StartByte);
        Assert.Equal(60 * 512, partition.Length);
    }

    [Fact]
    public async Task TreatsMissingSignatureAsRaw()
    {
        var disk = new byte[4096];
        var layout = await PartitionTableParser.ParseAsync(new MemoryStream(disk), disk.Length, 512);
        Assert.Equal(PartitionScheme.Raw, layout.Scheme);
        Assert.Empty(layout.Partitions);
        Assert.Equal(disk.Length, layout.LastAllocatedByte);
    }
}


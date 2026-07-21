using System.IO.Compression;
using MultiDiskImager.Core;

namespace MultiDiskImager.Core.Tests;

public sealed class DiskImageSourceTests
{
    [Fact]
    public async Task RawSourceRemainsASeekableByteForByteStream()
    {
        var path = Path.Combine(Path.GetTempPath(), $"multi-disk-imager-{Guid.NewGuid():N}.img");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);

            var source = DiskImageSource.Open(path);
            await using var stream = source.OpenSeekableRead();

            Assert.False(source.IsZipArchive);
            Assert.Equal(4, source.Length);
            stream.Position = 2;
            Assert.Equal(3, stream.ReadByte());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ZipSourceUsesFirstImgEntryCaseInsensitively()
    {
        var path = Path.Combine(Path.GetTempPath(), $"multi-disk-imager-{Guid.NewGuid():N}.zip");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "readme.txt", [9]);
                WriteEntry(archive, "images/first.IMG", [1, 2, 3, 4, 5]);
                WriteEntry(archive, "second.img", [6, 7, 8]);
            }

            var source = DiskImageSource.Open(path);

            Assert.True(source.IsZipArchive);
            Assert.Equal("images/first.IMG", source.EntryName);
            Assert.Equal(5, source.Length);
            await using var stream = source.OpenRead();
            Assert.False(stream.CanSeek);
            using var output = new MemoryStream();
            await stream.CopyToAsync(output);
            Assert.Equal([1, 2, 3, 4, 5], output.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ZipSourceCanReopenWhenSeekingBackwards()
    {
        var path = Path.Combine(Path.GetTempPath(), $"multi-disk-imager-{Guid.NewGuid():N}.zip");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "disk.img", [10, 20, 30, 40, 50]);
            }

            var source = DiskImageSource.Open(path);
            await using var stream = source.OpenSeekableRead();

            stream.Position = 3;
            Assert.Equal(40, stream.ReadByte());
            stream.Position = 0;
            Assert.Equal(10, stream.ReadByte());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ZipSourceRequiresAnImgEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"multi-disk-imager-{Guid.NewGuid():N}.zip");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "readme.txt", [1, 2, 3]);
            }

            var exception = Assert.Throws<InvalidDataException>(() => DiskImageSource.Open(path));
            Assert.Contains("does not contain an .img", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task TailDetectionSupportsAStreamingZipEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"multi-disk-imager-{Guid.NewGuid():N}.zip");
        try
        {
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "disk.img", [0, 0, 0, 0, 7]);
            }

            var source = DiskImageSource.Open(path);
            await using var stream = source.OpenRead();
            Assert.True(await new ImagingEngine(2).TailContainsNonZeroAsync(stream, 3));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static void WriteEntry(ZipArchive archive, string name, byte[] contents)
    {
        using var stream = archive.CreateEntry(name).Open();
        stream.Write(contents);
    }
}

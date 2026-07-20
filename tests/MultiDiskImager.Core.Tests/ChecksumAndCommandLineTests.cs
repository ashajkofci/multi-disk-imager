using System.Text;
using MultiDiskImager.Core;

namespace MultiDiskImager.Core.Tests;

public sealed class ChecksumAndCommandLineTests
{
    [Theory]
    [InlineData(ChecksumAlgorithm.Md5, "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData(ChecksumAlgorithm.Sha1, "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData(ChecksumAlgorithm.Sha256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public async Task ComputesKnownChecksumVectors(ChecksumAlgorithm algorithm, string expected)
    {
        await using var stream = new MemoryStream(Encoding.ASCII.GetBytes("abc"));
        Assert.Equal(expected, await ChecksumService.ComputeAsync(stream, algorithm));
    }

    [Fact]
    public void ParsesLegacyAndLongCommandLineAliases()
    {
        var options = CommandLineOptions.Parse(["-i", "image.img", "-d", "disk2", "disk3", "-w", "-v", "-s"]);
        Assert.Equal("image.img", options.ImagePath);
        Assert.Equal(["disk2", "disk3"], options.Devices);
        Assert.True(options.Write);
        Assert.True(options.Verify);
        Assert.True(options.AutoStart);
    }

    [Theory]
    [InlineData("-z")]
    [InlineData("--encryption")]
    public void RejectsRemovedContainerFeatures(string argument)
    {
        var exception = Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse([argument, "on"]));
        Assert.Contains("raw, unencrypted", exception.Message);
    }
}

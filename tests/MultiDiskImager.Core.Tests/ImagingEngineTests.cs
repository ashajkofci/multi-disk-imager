using MultiDiskImager.Core;

namespace MultiDiskImager.Core.Tests;

public sealed class ImagingEngineTests
{
    [Fact]
    public async Task CopyToDevicesCopiesRawBytesAndPadsFinalSector()
    {
        var input = Enumerable.Range(0, 1001).Select(index => (byte)(index % 251)).ToArray();
        await using var source = new MemoryStream(input, writable: false);
        await using var first = new MemoryStream();
        await using var second = new MemoryStream();
        var engine = new ImagingEngine(512);

        var result = await engine.CopyToDevicesAsync(
            source,
            new Dictionary<string, Stream> { ["one"] = first, ["two"] = second },
            input.Length,
            512,
            ImagingOperation.Write);

        Assert.True(result.Success);
        Assert.Equal(1024, first.Length);
        Assert.Equal(first.ToArray(), second.ToArray());
        Assert.Equal(input, first.ToArray()[..input.Length]);
        Assert.All(first.ToArray()[input.Length..], value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task CopyContinuesWhenOneDestinationFails()
    {
        var input = new byte[2048];
        Random.Shared.NextBytes(input);
        await using var source = new MemoryStream(input, writable: false);
        await using var good = new MemoryStream();
        await using var failing = new FailingWriteStream(700);
        var engine = new ImagingEngine(512);

        var result = await engine.CopyToDevicesAsync(
            source,
            new Dictionary<string, Stream> { ["good"] = good, ["bad"] = failing },
            input.Length,
            512,
            ImagingOperation.Write);

        Assert.True(result.PartialSuccess);
        Assert.Equal(input, good.ToArray());
        Assert.True(result.Devices.Single(device => device.DeviceId == "good").Success);
        Assert.False(result.Devices.Single(device => device.DeviceId == "bad").Success);
    }

    [Fact]
    public async Task CopyReportsZeroSpeedWhenADeviceCompletes()
    {
        var input = new byte[2048];
        await using var source = new MemoryStream(input, writable: false);
        await using var destination = new MemoryStream();
        var reports = new List<ImagingProgress>();
        var progress = new InlineProgress<ImagingProgress>(reports.Add);

        await new ImagingEngine(512).CopyToDevicesAsync(
            source,
            new Dictionary<string, Stream> { ["device"] = destination },
            input.Length,
            512,
            ImagingOperation.Write,
            progress);

        var completed = Assert.Single(reports, report => report.DeviceId == "device" && report.Fraction == 1);
        Assert.Equal("Complete", completed.Stage);
        Assert.Equal(0, completed.BytesPerSecond);
    }

    [Fact]
    public async Task VerifyReportsFirstMismatchForEachDevice()
    {
        var image = Enumerable.Range(0, 4096).Select(index => (byte)(index % 253)).ToArray();
        var mismatch = image.ToArray();
        mismatch[1234] ^= 0xFF;
        await using var imageStream = new MemoryStream(image, writable: false);
        await using var matchingStream = new MemoryStream(image, writable: false);
        await using var mismatchStream = new MemoryStream(mismatch, writable: false);
        var engine = new ImagingEngine(512);

        var result = await engine.VerifyDevicesAsync(
            imageStream,
            new Dictionary<string, Stream> { ["matching"] = matchingStream, ["mismatch"] = mismatchStream },
            image.Length);

        Assert.False(result.Success);
        Assert.True(result.Devices.Single(device => device.DeviceId == "matching").Success);
        Assert.Equal(1234, result.Devices.Single(device => device.DeviceId == "mismatch").FirstMismatchOffset);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TailDetectionFindsOnlyNonZeroData(bool nonZero)
    {
        var image = new byte[4096];
        if (nonZero)
        {
            image[3500] = 1;
        }

        await using var stream = new MemoryStream(image);
        var result = await new ImagingEngine(512).TailContainsNonZeroAsync(stream, 2048);
        Assert.Equal(nonZero, result);
    }

    [Fact]
    public async Task QuickWipeZerosTheStartAndEndButPreservesMiddle()
    {
        var data = Enumerable.Repeat((byte)0xA5, 4096).ToArray();
        await using var stream = new MemoryStream(data, writable: true);

        await new ImagingEngine().QuickWipeAsync(stream, data.Length, 512);

        var result = stream.ToArray();
        Assert.All(result[..512], value => Assert.Equal(0, value));
        Assert.All(result[512..^512], value => Assert.Equal(0xA5, value));
        Assert.All(result[^512..], value => Assert.Equal(0, value));
    }

    [Fact]
    public async Task QuickWipeDoesNotExtendADeviceWhenWipeRegionsOverlap()
    {
        var data = Enumerable.Repeat((byte)0xA5, 20).ToArray();
        await using var stream = new MemoryStream(data, writable: true);

        await new ImagingEngine().QuickWipeAsync(stream, data.Length, 16);

        Assert.Equal(20, stream.Length);
        Assert.All(stream.ToArray(), value => Assert.Equal(0, value));
    }

    private sealed class FailingWriteStream(long failAfter) : MemoryStream
    {
        private long _written;

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_written + buffer.Length > failAfter)
            {
                throw new IOException("Synthetic destination failure.");
            }

            await base.WriteAsync(buffer, cancellationToken);
            _written += buffer.Length;
        }
    }

    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}

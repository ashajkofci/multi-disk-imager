using System.Buffers;
using System.Security.Cryptography;

namespace MultiDiskImager.Core;

public enum ChecksumAlgorithm
{
    Md5,
    Sha1,
    Sha256
}

public static class ChecksumService
{
    public static async Task<string> ComputeAsync(
        Stream stream,
        ChecksumAlgorithm algorithm,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default,
        long? totalLength = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("Stream is not readable.", nameof(stream));
        }

        using var hash = algorithm switch
        {
            ChecksumAlgorithm.Md5 => IncrementalHash.CreateHash(HashAlgorithmName.MD5),
            ChecksumAlgorithm.Sha1 => IncrementalHash.CreateHash(HashAlgorithmName.SHA1),
            ChecksumAlgorithm.Sha256 => IncrementalHash.CreateHash(HashAlgorithmName.SHA256),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };

        var buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024);
        long processed = 0;
        var total = totalLength ?? (stream.CanSeek ? stream.Length - stream.Position : 0);
        try
        {
            int read;
            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                processed += read;
                if (total > 0)
                {
                    progress?.Report(Math.Clamp((double)processed / total, 0, 1));
                }
            }

            progress?.Report(1);
            return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }
}

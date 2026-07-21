using System.IO.Compression;

namespace MultiDiskImager.Core;

public sealed class DiskImageSource
{
    private readonly int? _zipEntryIndex;

    private DiskImageSource(string path, long length, int? zipEntryIndex, string? entryName)
    {
        Path = path;
        Length = length;
        _zipEntryIndex = zipEntryIndex;
        EntryName = entryName;
    }

    public string Path { get; }
    public long Length { get; }
    public string? EntryName { get; }
    public bool IsZipArchive => _zipEntryIndex.HasValue;

    public static bool IsZipPath(string path) =>
        System.IO.Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase);

    public static DiskImageSource Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!IsZipPath(path))
        {
            return new DiskImageSource(path, new FileInfo(path).Length, null, null);
        }

        using var archive = ZipFile.OpenRead(path);
        for (var index = 0; index < archive.Entries.Count; index++)
        {
            var entry = archive.Entries[index];
            if (!string.IsNullOrEmpty(entry.Name) &&
                System.IO.Path.GetExtension(entry.Name).Equals(".img", StringComparison.OrdinalIgnoreCase))
            {
                return new DiskImageSource(path, entry.Length, index, entry.FullName);
            }
        }

        throw new InvalidDataException("The ZIP archive does not contain an .img file.");
    }

    public Stream OpenRead()
    {
        if (_zipEntryIndex is not { } entryIndex)
        {
            return new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        var file = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            var archive = new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true);
            try
            {
                if (entryIndex >= archive.Entries.Count)
                {
                    throw new IOException("The ZIP archive changed after it was selected.");
                }

                var entry = archive.Entries[entryIndex];
                if (!entry.FullName.Equals(EntryName, StringComparison.Ordinal) || entry.Length != Length)
                {
                    throw new IOException("The ZIP archive changed after it was selected.");
                }

                return new OwnedZipEntryStream(entry.Open(), archive, file, Length);
            }
            catch
            {
                archive.Dispose();
                throw;
            }
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    public Stream OpenSeekableRead() => IsZipArchive
        ? new ReopenableReadStream(OpenRead, Length)
        : OpenRead();

    private sealed class OwnedZipEntryStream(Stream inner, ZipArchive archive, FileStream file, long length) : Stream
    {
        private long _position;
        private bool _disposed;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position
        {
            get => _position;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = inner.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            var read = inner.Read(buffer);
            _position += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                inner.Dispose();
                archive.Dispose();
                file.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await inner.DisposeAsync().ConfigureAwait(false);
                archive.Dispose();
                await file.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }
    }

    private sealed class ReopenableReadStream(Func<Stream> open, long length) : Stream
    {
        private Stream _inner = open();
        private long _position;
        private bool _disposed;

        public override bool CanRead => !_disposed;
        public override bool CanSeek => !_disposed;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position
        {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var read = _inner.Read(buffer, offset, count);
            _position += read;
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var read = _inner.Read(buffer);
            _position += read;
            return read;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var read = await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            _position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(_position + offset),
                SeekOrigin.End => checked(length + offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            if (target < 0 || target > length)
            {
                throw new IOException("Attempted to seek outside the image.");
            }

            if (target < _position)
            {
                _inner.Dispose();
                _inner = open();
                _position = 0;
            }

            if (target > _position)
            {
                var buffer = new byte[1024 * 1024];
                while (_position < target)
                {
                    var requested = (int)Math.Min(buffer.Length, target - _position);
                    var read = Read(buffer, 0, requested);
                    if (read == 0)
                    {
                        throw new EndOfStreamException("The image ended before the requested position.");
                    }
                }
            }

            return _position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
                await _inner.DisposeAsync().ConfigureAwait(false);
            }

            GC.SuppressFinalize(this);
        }
    }
}

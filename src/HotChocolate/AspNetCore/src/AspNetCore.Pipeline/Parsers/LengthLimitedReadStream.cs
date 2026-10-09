namespace HotChocolate.AspNetCore.Parsers;

/// <summary>
/// A read-only stream over another stream that throws the exception
/// <paramref name="createLimitExceededException"/> creates once more than
/// <paramref name="limit"/> bytes have been read through it.
/// </summary>
internal sealed class LengthLimitedReadStream(
    Stream innerStream,
    long limit,
    Func<Exception> createLimitExceededException)
    : Stream
{
    private long _bytesRead;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        var read = innerStream.Read(buffer);
        CountBytesRead(read);

        return read;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        var read = await innerStream.ReadAsync(buffer, cancellationToken);
        CountBytesRead(read);

        return read;
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    private void CountBytesRead(int read)
    {
        _bytesRead += read;

        if (_bytesRead > limit)
        {
            throw createLimitExceededException();
        }
    }
}

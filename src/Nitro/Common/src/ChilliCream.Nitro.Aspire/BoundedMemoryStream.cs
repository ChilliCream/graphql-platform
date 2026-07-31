namespace ChilliCream.Nitro.Aspire;

internal sealed class BoundedMemoryStream(int maximumLength)
    : MemoryStream
{
    public override void SetLength(long value)
    {
        EnsureCapacity(value);
        base.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        EnsureWrite(count);
        base.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        EnsureWrite(buffer.Length);
        base.Write(buffer);
    }

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
    {
        EnsureWrite(count);
        return base.WriteAsync(
            buffer,
            offset,
            count,
            cancellationToken);
    }

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        EnsureWrite(buffer.Length);
        return base.WriteAsync(buffer, cancellationToken);
    }

    public override void WriteByte(byte value)
    {
        EnsureWrite(1);
        base.WriteByte(value);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Array.Clear(GetBuffer());
        }

        base.Dispose(disposing);
    }

    private void EnsureWrite(int count)
        => EnsureCapacity(checked(Position + count));

    private void ThrowLimitExceeded()
        => throw new InvalidDataException(
            "The composed Fusion archive exceeds the "
            + $"{maximumLength:N0}-byte in-memory size limit.");

    private void EnsureCapacity(long length)
    {
        if (length > maximumLength)
        {
            ThrowLimitExceeded();
        }
    }
}

internal sealed record FusionPipelineMemoryLimits(
    int SourceArchiveBytes,
    long TotalSourceArchiveBytes,
    int FusionArchiveBytes)
{
    public const int DefaultSourceArchiveBytes = 128_000_000;

    public const int DefaultTotalSourceArchiveBytes = 512_000_000;

    public const int DefaultFusionArchiveBytes = 256_000_000;

    public static FusionPipelineMemoryLimits Default { get; } = new(
        DefaultSourceArchiveBytes,
        DefaultTotalSourceArchiveBytes,
        DefaultFusionArchiveBytes);
}

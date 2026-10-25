using System.IO.Pipelines;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// A pipe writer that forwards to <paramref name="inner"/> and counts the calls it receives.
/// </summary>
internal sealed class CountingPipeWriter(PipeWriter inner) : PipeWriter
{
    /// <summary>
    /// Gets the number of <see cref="GetSpan"/> and <see cref="GetMemory"/> calls.
    /// </summary>
    public int SpanCount { get; private set; }

    /// <summary>
    /// Gets the largest size hint passed to <see cref="GetSpan"/> or <see cref="GetMemory"/>.
    /// </summary>
    public int MaxSizeHint { get; private set; }

    /// <summary>
    /// Gets the number of <see cref="Advance"/> calls.
    /// </summary>
    public int AdvanceCount { get; private set; }

    /// <summary>
    /// Gets the number of <see cref="FlushAsync"/> calls.
    /// </summary>
    public int FlushCount { get; private set; }

    public override void Advance(int bytes)
    {
        AdvanceCount++;
        inner.Advance(bytes);
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        SpanCount++;
        MaxSizeHint = Math.Max(MaxSizeHint, sizeHint);
        return inner.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        SpanCount++;
        MaxSizeHint = Math.Max(MaxSizeHint, sizeHint);
        return inner.GetSpan(sizeHint);
    }

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        FlushCount++;
        return inner.FlushAsync(cancellationToken);
    }

    public override void CancelPendingFlush() => inner.CancelPendingFlush();

    public override void Complete(Exception? exception = null) => inner.Complete(exception);
}

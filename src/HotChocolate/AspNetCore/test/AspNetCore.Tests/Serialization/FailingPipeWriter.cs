using System.IO.Pipelines;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// A pipe writer that forwards to <paramref name="inner"/> and whose first flush invokes
/// <paramref name="onFlush"/> and then fails with an <see cref="IOException"/>.
/// </summary>
internal sealed class FailingPipeWriter(PipeWriter inner, Action onFlush) : PipeWriter
{
    /// <summary>
    /// The message of the exception that the first flush throws.
    /// </summary>
    public const string Message = "The connection was lost.";

    private bool _failed;

    public override void Advance(int bytes) => inner.Advance(bytes);

    public override Memory<byte> GetMemory(int sizeHint = 0) => inner.GetMemory(sizeHint);

    public override Span<byte> GetSpan(int sizeHint = 0) => inner.GetSpan(sizeHint);

    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_failed)
        {
            return inner.FlushAsync(cancellationToken);
        }

        _failed = true;
        onFlush();
        throw new IOException(Message);
    }

    public override void CancelPendingFlush() => inner.CancelPendingFlush();

    public override void Complete(Exception? exception = null) => inner.Complete(exception);
}

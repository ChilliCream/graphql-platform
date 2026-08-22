using System.Globalization;

#if FUSION
namespace HotChocolate.Fusion.Transport.Http;
#else
namespace HotChocolate.Transport.Http;
#endif

/// <summary>
/// A read-only stream that fails a read with a <see cref="TimeoutException"/> when the inner stream
/// does not complete that read within the configured timeout. Every read, including a zero-byte read,
/// starts a new timeout window.
/// </summary>
internal sealed class ReadTimeoutStream : Stream
{
    /// <summary>
    /// The shortest supported timeout.
    /// </summary>
    public static readonly TimeSpan MinTimeout = TimeSpan.FromMilliseconds(1);

    /// <summary>
    /// The longest supported timeout.
    /// </summary>
    public static readonly TimeSpan MaxTimeout = TimeSpan.FromMilliseconds(uint.MaxValue - 1);

    private readonly Stream _inner;
    private readonly TimeSpan _timeout;
    private CancellationTokenSource _timeoutSource = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of <see cref="ReadTimeoutStream"/>.
    /// </summary>
    /// <param name="inner">The stream to read from. It is disposed together with this stream.</param>
    /// <param name="timeout">
    /// The maximum time a single read may take. Must be between <see cref="MinTimeout"/> and
    /// <see cref="MaxTimeout"/>, inclusive.
    /// </param>
    public ReadTimeoutStream(Stream inner, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(inner);

        if (!IsValidTimeout(timeout))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "The read timeout must be between 1 millisecond and uint.MaxValue - 1 milliseconds.");
        }

        _inner = inner;
        _timeout = timeout;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="timeout"/> lies between <see cref="MinTimeout"/> and
    /// <see cref="MaxTimeout"/>, inclusive.
    /// </summary>
    public static bool IsValidTimeout(TimeSpan timeout)
        => timeout >= MinTimeout && timeout <= MaxTimeout;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override async ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var timeoutSource = _timeoutSource;
        var registration = cancellationToken.CanBeCanceled
            ? cancellationToken.UnsafeRegister(
                static state => ((CancellationTokenSource)state!).Cancel(),
                timeoutSource)
            : default;

        try
        {
            timeoutSource.CancelAfter(_timeout);
            return await _inner.ReadAsync(buffer, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (Exception exception) when (timeoutSource.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested
            && !_disposed)
        {
            throw new TimeoutException(
                "No data (including keep-alive messages) was received on the response stream "
                + $"within the configured read timeout of {FormatTimeout(_timeout)}.",
                exception);
        }
        finally
        {
            registration.Dispose();
            ResetTimeout(timeoutSource);
        }
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken)
        => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override int Read(byte[] buffer, int offset, int count)
        => throw new NotSupportedException("Only asynchronous reads are supported.");

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (disposing)
        {
            _inner.Dispose();
            _timeoutSource.Dispose();
        }

        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await _inner.DisposeAsync().ConfigureAwait(false);
        _timeoutSource.Dispose();
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private void ResetTimeout(CancellationTokenSource timeoutSource)
    {
        if (_disposed)
        {
            return;
        }

        // A source whose timer already fired cannot be reused. Replacing it keeps a read that merely
        // completed late, or a consumer that paused between two reads, from timing out the next read.
        if (!timeoutSource.TryReset())
        {
            timeoutSource.Dispose();
            _timeoutSource = new CancellationTokenSource();
        }
    }

    private static string FormatTimeout(TimeSpan timeout)
        => timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) + " seconds";
}

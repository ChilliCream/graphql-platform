using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace HotChocolate.Transport.Formatters;

/// <summary>
/// A pipe writer that buffers small writes and copies them to the underlying pipe when the
/// buffer is full or a flush is requested. A write of <see cref="DirectWriteThreshold"/> bytes
/// or more goes to the pipe directly. The buffer is held only between the first buffered write
/// and the next flush. It is not thread safe.
/// </summary>
internal sealed class ChunkedPipeBufferWriter : PipeWriter, IDisposable
{
    private const int ChunkSize = 64 * 1024;

    /// <summary>
    /// The size hint from which a write bypasses the buffer and goes to the pipe.
    /// </summary>
    internal const int DirectWriteThreshold = 4 * 1024;

    [ThreadStatic]
    private static ChunkedPipeBufferWriter? s_cached;

    private PipeWriter _pipe = default!;
    private byte[] _buffer = [];
    private int _position;
    private bool _writeToPipe;
    private bool _rented;

    private ChunkedPipeBufferWriter()
    {
    }

    /// <summary>
    /// Gets a writer for <paramref name="pipe"/>. The writer must be disposed after its last flush.
    /// </summary>
    public static ChunkedPipeBufferWriter Rent(PipeWriter pipe)
    {
        var writer = s_cached ?? new ChunkedPipeBufferWriter();
        s_cached = null;
        writer._pipe = pipe;
        writer._rented = true;
        return writer;
    }

    /// <summary>
    /// Gets a value indicating whether the writer currently holds a buffer.
    /// </summary>
    internal bool HasBuffer => _buffer.Length > 0;

    public override void Advance(int bytes)
    {
        if (_writeToPipe)
        {
            _writeToPipe = false;
            _pipe.Advance(bytes);
            return;
        }

        if ((uint)bytes > (uint)(_buffer.Length - _position))
        {
            ThrowAdvanceOutOfRange(bytes);
        }

        _position += bytes;
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        if (EnsureCapacity(sizeHint))
        {
            return _buffer.AsMemory(_position);
        }

        return _pipe.GetMemory(sizeHint);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        if (EnsureCapacity(sizeHint))
        {
            return _buffer.AsSpan(_position);
        }

        return _pipe.GetSpan(sizeHint);
    }

    /// <summary>
    /// Copies the buffered bytes to the pipe, releases the buffer and flushes the pipe.
    /// </summary>
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        CopyToPipe();
        ReleaseBuffer();
        return _pipe.FlushAsync(cancellationToken);
    }

    public override void CancelPendingFlush() => _pipe.CancelPendingFlush();

    public override void Complete(Exception? exception = null)
    {
        if (exception is null)
        {
            CopyToPipe();
        }

        ReleaseBuffer();
        _pipe.Complete(exception);
    }

    /// <summary>
    /// Discards the buffered bytes that have not been copied to the pipe yet.
    /// Bytes that were already copied to the pipe are kept.
    /// </summary>
    public void Discard()
    {
        _writeToPipe = false;
        ReleaseBuffer();
    }

    private void CopyToPipe()
    {
        if (_position > 0)
        {
            _pipe.Write(_buffer.AsSpan(0, _position));
            _position = 0;
        }
    }

    private void ReleaseBuffer()
    {
        if (_buffer.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = [];
            _position = 0;
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the buffer serves <paramref name="sizeHint"/> bytes and
    /// <c>false</c> when the next write and its <see cref="Advance"/> go to the pipe.
    /// </summary>
    private bool EnsureCapacity(int sizeHint)
    {
        _writeToPipe = false;

        if (sizeHint >= DirectWriteThreshold)
        {
            CopyToPipe();
            _writeToPipe = true;
            return false;
        }

        if (sizeHint < 1)
        {
            sizeHint = 1;
        }

        if (_buffer.Length == 0)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
        }
        else if (_buffer.Length - _position < sizeHint)
        {
            CopyToPipe();
        }

        return true;
    }

    [DoesNotReturn]
    private static void ThrowAdvanceOutOfRange(int bytes)
    {
        if (bytes < 0)
        {
            throw ThrowHelper.ChunkedPipeBufferWriter_AdvanceNegative(bytes);
        }

        throw ThrowHelper.ChunkedPipeBufferWriter_AdvancePastBuffer();
    }

    /// <summary>
    /// Releases the writer for reuse. Bytes that were not flushed are discarded.
    /// </summary>
    public void Dispose()
    {
        if (!_rented)
        {
            return;
        }

        ReleaseBuffer();
        _pipe = default!;
        _writeToPipe = false;
        _rented = false;
        s_cached ??= this;
    }
}

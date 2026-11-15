using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace HotChocolate.Transport.Formatters;

/// <summary>
/// A pipe writer that buffers small writes and copies them to the underlying pipe when the
/// buffer is full or a flush is requested. A write of <see cref="DirectWriteThreshold"/> bytes
/// or more goes to the pipe directly. When the writer is rented with span growth, once such a
/// write fills its span, the next one asks the pipe for a span that grows with the bytes written
/// since the last flush. The buffer is held only between the first buffered write and the next
/// flush. It is not thread safe.
/// </summary>
internal sealed class ChunkedPipeBufferWriter : PipeWriter, IDisposable
{
    private const int ChunkSize = 64 * 1024;

    /// <summary>
    /// The size hint from which a write bypasses the buffer and goes to the pipe.
    /// </summary>
    internal const int DirectWriteThreshold = 2 * 1024;

    /// <summary>
    /// The largest size hint that a direct write asks the pipe for beyond the caller's own hint.
    /// </summary>
    internal const int MaxPipeSizeHint = 16 * 1024;

    [ThreadStatic]
    private static ChunkedPipeBufferWriter? s_cached;

    private PipeWriter _pipe = default!;
    private byte[] _buffer = [];
    private int _position;
    private bool _writeToPipe;
    private bool _rented;
    private long _unflushedBytes;
    private int _pipeSpanLength;
    private bool _pipeSpanFilled;
    private bool _growPipeSpans;

    private ChunkedPipeBufferWriter()
    {
    }

    /// <summary>
    /// Gets a writer for <paramref name="pipe"/>. The writer must be disposed after its last flush.
    /// </summary>
    /// <param name="pipe">
    /// The pipe that receives the written bytes.
    /// </param>
    /// <param name="growPipeSpans">
    /// <c>true</c> if direct writes that fill their spans ask <paramref name="pipe"/> for growing
    /// spans; <c>false</c> if every direct write forwards the caller's size hint unchanged.
    /// </param>
    public static ChunkedPipeBufferWriter Rent(PipeWriter pipe, bool growPipeSpans)
    {
        var writer = s_cached ?? new ChunkedPipeBufferWriter();
        s_cached = null;
        writer._pipe = pipe;
        writer._growPipeSpans = growPipeSpans;
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
            _unflushedBytes += bytes;
            _pipeSpanFilled = _pipeSpanLength - bytes < DirectWriteThreshold;
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

        var memory = _pipe.GetMemory(GetPipeSizeHint(sizeHint));
        _pipeSpanLength = memory.Length;
        return memory;
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        if (EnsureCapacity(sizeHint))
        {
            return _buffer.AsSpan(_position);
        }

        var span = _pipe.GetSpan(GetPipeSizeHint(sizeHint));
        _pipeSpanLength = span.Length;
        return span;
    }

    /// <summary>
    /// Copies the buffered bytes to the pipe, releases the buffer and flushes the pipe.
    /// </summary>
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        CopyToPipe();
        ReleaseBuffer();
        ResetPipeSizeHint();
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
        ResetPipeSizeHint();
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
            _unflushedBytes += _position;
            _pipeSpanFilled = false;
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
    /// Gets the size hint of a direct write. It is at least <paramref name="sizeHint"/>, and when
    /// the writer grows pipe spans and a direct write filled its span, it grows with the bytes
    /// written since the last flush, up to <see cref="MaxPipeSizeHint"/>.
    /// </summary>
    private int GetPipeSizeHint(int sizeHint)
    {
        if (!_growPipeSpans || !_pipeSpanFilled)
        {
            return sizeHint;
        }

        var unflushedBytes = (int)Math.Min(_unflushedBytes, MaxPipeSizeHint);
        return Math.Max(sizeHint, unflushedBytes);
    }

    private void ResetPipeSizeHint()
    {
        _unflushedBytes = 0;
        _pipeSpanFilled = false;
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
        ResetPipeSizeHint();
        _pipe = default!;
        _writeToPipe = false;
        _growPipeSpans = false;
        _rented = false;
        s_cached ??= this;
    }
}

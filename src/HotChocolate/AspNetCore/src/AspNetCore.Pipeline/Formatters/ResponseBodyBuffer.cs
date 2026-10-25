using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using HotChocolate.AspNetCore.Utilities;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// A pipe writer that holds a complete response body in memory until it is copied to the
/// response. It is not thread safe.
/// </summary>
internal sealed class ResponseBodyBuffer : PipeWriter, IDisposable
{
    private const int ChunkSize = 64 * 1024;
    private const int MaxCopySize = 64 * 1024;
    private const int MaxCopyPerCall = 1024 * 1024;

    [ThreadStatic]
    private static ResponseBodyBuffer? s_cached;

    private Chunk[] _chunks = new Chunk[8];
    private int _chunkCount;
    private long _chunkBytes;
    private byte[] _current = [];
    private int _position;
    private int _copyIndex;
    private int _copyOffset;
    private long _copiedBytes;
    private bool _completed;
    private bool _cancelNextFlush;
    private Exception? _completionException;
    private bool _rented;

    private ResponseBodyBuffer()
    {
    }

    /// <summary>
    /// Gets an empty buffer. The buffer must be disposed once its body is no longer needed.
    /// </summary>
    public static ResponseBodyBuffer Rent()
    {
        var buffer = s_cached ?? new ResponseBodyBuffer();
        s_cached = null;
        buffer._rented = true;
        return buffer;
    }

    /// <summary>
    /// Gets the number of bytes written to the buffer.
    /// </summary>
    public long Length => _chunkBytes + _position;

    /// <summary>
    /// Gets the exception the writer was completed with, or <c>null</c> when it was not
    /// completed with an exception.
    /// </summary>
    public Exception? CompletionException => _completionException;

    public override void Advance(int bytes)
    {
        if ((uint)bytes > (uint)(_current.Length - _position))
        {
            ThrowAdvanceOutOfRange(bytes);
        }

        _position += bytes;
    }

    public override Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _current.AsMemory(_position);
    }

    public override Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _current.AsSpan(_position);
    }

    /// <summary>
    /// Returns a completed flush without sending the buffered bytes.
    /// </summary>
    public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<FlushResult>(cancellationToken);
        }

        var isCanceled = _cancelNextFlush;
        _cancelNextFlush = false;
        return new ValueTask<FlushResult>(new FlushResult(isCanceled, isCompleted: false));
    }

    public override void CancelPendingFlush() => _cancelNextFlush = true;

    public override void Complete(Exception? exception = null)
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        _completionException = exception;

        // Without a current chunk every later write ends up in Grow or ThrowAdvanceOutOfRange,
        // which reject it.
        SealCurrent();
    }

    /// <summary>
    /// Copies the next part of the body to <paramref name="destination"/> without flushing it
    /// and returns <c>true</c> while part of the body has not been copied yet. The buffer
    /// accepts no more writes once copying starts.
    /// </summary>
    public bool CopyTo(PipeWriter destination)
    {
        _completed = true;
        SealCurrent();

        var copied = 0;

        while (_copyIndex < _chunkCount && copied < MaxCopyPerCall)
        {
            var remaining = _chunkBytes - _copiedBytes;
            var target = destination.GetSpan((int)Math.Min(remaining, MaxCopySize));
            var written = 0;

            while (written < target.Length && _copyIndex < _chunkCount)
            {
                ref var chunk = ref _chunks[_copyIndex];
                var count = Math.Min(chunk.Length - _copyOffset, target.Length - written);
                chunk.Buffer.AsSpan(_copyOffset, count).CopyTo(target[written..]);
                written += count;
                _copyOffset += count;

                if (_copyOffset == chunk.Length)
                {
                    ArrayPool<byte>.Shared.Return(chunk.Buffer);
                    chunk = default;
                    _copyIndex++;
                    _copyOffset = 0;
                }
            }

            destination.Advance(written);
            copied += written;
            _copiedBytes += written;
        }

        return _copyIndex < _chunkCount;
    }

    /// <summary>
    /// Releases the buffer for reuse. The part of the body that was not copied is discarded.
    /// </summary>
    public void Dispose()
    {
        if (!_rented)
        {
            return;
        }

        for (var i = _copyIndex; i < _chunkCount; i++)
        {
            ArrayPool<byte>.Shared.Return(_chunks[i].Buffer);
        }

        if (_current.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_current);
            _current = [];
        }

        _chunks.AsSpan(0, _chunkCount).Clear();
        _position = 0;
        _chunkCount = 0;
        _chunkBytes = 0;
        _copyIndex = 0;
        _copyOffset = 0;
        _copiedBytes = 0;
        _completed = false;
        _cancelNextFlush = false;
        _completionException = null;
        _rented = false;
        s_cached ??= this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureCapacity(int sizeHint)
    {
        var available = _current.Length - _position;

        if (available == 0 || available < sizeHint)
        {
            Grow(sizeHint);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int sizeHint)
    {
        if (_completed)
        {
            throw ThrowHelper.ResponseBodyBuffer_WriterCompleted();
        }

        SealCurrent();
        _current = ArrayPool<byte>.Shared.Rent(Math.Max(sizeHint, ChunkSize));
    }

    /// <summary>
    /// Moves the written part of the current chunk to the chunk list and leaves the buffer
    /// without a current chunk.
    /// </summary>
    private void SealCurrent()
    {
        if (_position > 0)
        {
            if (_chunkCount == _chunks.Length)
            {
                Array.Resize(ref _chunks, _chunks.Length * 2);
            }

            _chunks[_chunkCount++] = new Chunk(_current, _position);
            _chunkBytes += _position;
            _position = 0;
        }
        else if (_current.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_current);
        }

        _current = [];
    }

    [DoesNotReturn]
    private void ThrowAdvanceOutOfRange(int bytes)
    {
        if (bytes < 0)
        {
            throw ThrowHelper.ResponseBodyBuffer_AdvanceNegative(bytes);
        }

        if (_completed)
        {
            throw ThrowHelper.ResponseBodyBuffer_WriterCompleted();
        }

        throw ThrowHelper.ResponseBodyBuffer_AdvancePastBuffer();
    }

    private readonly record struct Chunk(byte[] Buffer, int Length);
}

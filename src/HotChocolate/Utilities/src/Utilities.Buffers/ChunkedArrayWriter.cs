using System.Buffers;
using System.Runtime.CompilerServices;

namespace HotChocolate.Buffers;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> that grows by renting additional fixed-size chunks
/// from <see cref="JsonMemory"/> instead of resizing and copying like <see cref="PooledArrayWriter"/>.
/// Uses cursor-based addressing where a single <see cref="int"/> location maps to a chunk index
/// and offset via shift and mask operations on <see cref="JsonMemory.BufferSize"/>.
/// </summary>
internal sealed class ChunkedArrayWriter : IBufferWriter<byte>, IDisposable
{
    private const int BufferSize = JsonMemory.BufferSize;
    private const int BufferMask = BufferSize - 1;
    private const int BufferShift = 17; // log2(131072) = 17
    private const int DefaultScratchSize = 128;

    private byte[][] _chunks;
    private int _chunkCount;
    private int _currentChunk;
    private int _currentChunkOffset;
    private byte[] _scratch;
    private bool _advanceFromScratch;
    private bool _disposed;

    public ChunkedArrayWriter()
    {
        _chunks = ArrayPool<byte[]>.Shared.Rent(4);
        _chunks[0] = JsonMemory.Rent(JsonMemoryKind.Variables);
        _chunkCount = 1;
        _scratch = new byte[DefaultScratchSize];
    }

    /// <summary>
    /// Gets the current write position as a flat cursor.
    /// </summary>
    public int Position
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (_currentChunk << BufferShift) | _currentChunkOffset;
    }

    /// <summary>
    /// Gets the total number of bytes written.
    /// </summary>
    public int Length => Position;

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var size = sizeHint < 1 ? DefaultScratchSize : sizeHint;
        var chunk = _chunks[_currentChunk];
        var remaining = BufferSize - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            chunk = _chunks[_currentChunk];
            remaining = BufferSize;
        }

        if (size <= remaining)
        {
            return chunk.AsSpan(_currentChunkOffset);
        }

        // The requested size exceeds the remaining space in this chunk.
        // Return a scratch buffer; on Advance we copy into chunks.
        if (size > _scratch.Length)
        {
            _scratch = new byte[size];
        }

        _advanceFromScratch = true;
        return _scratch;
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        var size = sizeHint < 1 ? DefaultScratchSize : sizeHint;
        var chunk = _chunks[_currentChunk];
        var remaining = BufferSize - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            chunk = _chunks[_currentChunk];
            remaining = BufferSize;
        }

        if (size <= remaining)
        {
            return chunk.AsMemory(_currentChunkOffset);
        }

        if (size > _scratch.Length)
        {
            _scratch = new byte[size];
        }

        _advanceFromScratch = true;
        return _scratch;
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        if (!_advanceFromScratch)
        {
            _currentChunkOffset += count;
            return;
        }

        _advanceFromScratch = false;
        var source = _scratch.AsSpan(0, count);

        while (source.Length > 0)
        {
            var chunk = _chunks[_currentChunk];
            var remaining = BufferSize - _currentChunkOffset;

            if (remaining == 0)
            {
                MoveToNextChunk();
                chunk = _chunks[_currentChunk];
                remaining = BufferSize;
            }

            var take = Math.Min(source.Length, remaining);
            source.Slice(0, take).CopyTo(chunk.AsSpan(_currentChunkOffset, take));
            _currentChunkOffset += take;
            source = source.Slice(take);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Read(ref int start, ref int length)
    {
        var chunkIndex = start >> BufferShift;
        var offsetInChunk = start & BufferMask;
        var available = BufferSize - offsetInChunk;

        if (available >= length)
        {
            var span = _chunks[chunkIndex].AsSpan(offsetInChunk, length);
            length = 0;
            return span;
        }

        start = (chunkIndex + 1) << BufferShift;
        length -= available;
        return _chunks[chunkIndex].AsSpan(offsetInChunk, available);
    }

    /// <summary>
    /// Compares an external span against a written segment at the specified location.
    /// Handles segments that span chunk boundaries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEqual(ReadOnlySpan<byte> span, int location, int length)
    {
        if (span.Length != length)
        {
            return false;
        }

        if (length == 0)
        {
            return true;
        }

        var chunkIndex = location >> BufferShift;
        var offsetInChunk = location & BufferMask;
        var availableInChunk = BufferSize - offsetInChunk;

        // Fast path: segment is entirely within one chunk.
        if (availableInChunk >= length)
        {
            return span.SequenceEqual(
                _chunks[chunkIndex].AsSpan(offsetInChunk, length));
        }

        return SequenceEqualMultiChunk(span, chunkIndex, offsetInChunk, length);
    }

    private bool SequenceEqualMultiChunk(
        ReadOnlySpan<byte> span,
        int chunkIndex,
        int offsetInChunk,
        int remaining)
    {
        var spanOffset = 0;

        while (remaining > 0)
        {
            var available = BufferSize - offsetInChunk;
            var toCompare = Math.Min(remaining, available);

            if (!span.Slice(spanOffset, toCompare).SequenceEqual(
                    _chunks[chunkIndex].AsSpan(offsetInChunk, toCompare)))
            {
                return false;
            }

            spanOffset += toCompare;
            remaining -= toCompare;
            chunkIndex++;
            offsetInChunk = 0;
        }

        return true;
    }

    /// <summary>
    /// Compares two written segments for equality.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEqual(int locationA, int locationB, int length)
    {
        if (locationA == locationB)
        {
            return true;
        }

        if (length == 0)
        {
            return true;
        }

        var chunkA = locationA >> BufferShift;
        var offsetA = locationA & BufferMask;
        var availA = BufferSize - offsetA;
        var chunkB = locationB >> BufferShift;
        var offsetB = locationB & BufferMask;
        var availB = BufferSize - offsetB;

        // Fast path: both segments within their respective single chunks.
        if (availA >= length && availB >= length)
        {
            return _chunks[chunkA].AsSpan(offsetA, length).SequenceEqual(
                _chunks[chunkB].AsSpan(offsetB, length));
        }

        return SequenceEqualMultiChunkTwoSegments(chunkA, offsetA, chunkB, offsetB, length);
    }

    private bool SequenceEqualMultiChunkTwoSegments(
        int chunkA, int offsetA,
        int chunkB, int offsetB,
        int remaining)
    {
        while (remaining > 0)
        {
            var availA = BufferSize - offsetA;
            var availB = BufferSize - offsetB;
            var toCompare = Math.Min(remaining, Math.Min(availA, availB));

            if (!_chunks[chunkA].AsSpan(offsetA, toCompare).SequenceEqual(
                    _chunks[chunkB].AsSpan(offsetB, toCompare)))
            {
                return false;
            }

            remaining -= toCompare;
            offsetA += toCompare;
            offsetB += toCompare;

            if (offsetA >= BufferSize)
            {
                chunkA++;
                offsetA = 0;
            }

            if (offsetB >= BufferSize)
            {
                chunkB++;
                offsetB = 0;
            }
        }

        return true;
    }

    /// <summary>
    /// Computes a hash code for a written segment using <c>hash * 31 + b</c>.
    /// Handles segments that span chunk boundaries.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(int location, int length)
    {
        if (length == 0)
        {
            return 0;
        }

        var chunkIndex = location >> BufferShift;
        var offsetInChunk = location & BufferMask;
        var availableInChunk = BufferSize - offsetInChunk;

        // Fast path: segment is entirely within one chunk.
        if (availableInChunk >= length)
        {
            return ComputeHash(
                _chunks[chunkIndex].AsSpan(offsetInChunk, length));
        }

        return GetHashCodeMultiChunk(chunkIndex, offsetInChunk, length);
    }

    private int GetHashCodeMultiChunk(int chunkIndex, int offsetInChunk, int remaining)
    {
        var hash = 0u;

        while (remaining > 0)
        {
            var available = BufferSize - offsetInChunk;
            var toHash = Math.Min(remaining, available);

            foreach (var b in _chunks[chunkIndex].AsSpan(offsetInChunk, toHash))
            {
                hash = hash * 31 + b;
            }

            remaining -= toHash;
            chunkIndex++;
            offsetInChunk = 0;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHash(ReadOnlySpan<byte> bytes)
    {
        var hash = 0u;

        foreach (var b in bytes)
        {
            hash = hash * 31 + b;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    /// <summary>
    /// Writes a previously written segment to the specified target buffer writer.
    /// Handles segments that span chunk boundaries.
    /// </summary>
    public void WriteTo(IBufferWriter<byte> target, int location, int length)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(target);
#else
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }
#endif

        var remaining = length;
        var chunkIndex = location >> BufferShift;
        var offsetInChunk = location & BufferMask;

        while (remaining > 0)
        {
            var available = BufferSize - offsetInChunk;
            var toCopy = Math.Min(remaining, available);

            var destination = target.GetSpan(toCopy);
            _chunks[chunkIndex].AsSpan(offsetInChunk, toCopy).CopyTo(destination);
            target.Advance(toCopy);

            remaining -= toCopy;
            chunkIndex++;
            offsetInChunk = 0;
        }
    }

    /// <summary>
    /// Copies a written segment to the specified destination span.
    /// Handles segments that span chunk boundaries.
    /// </summary>
    public void CopyTo(Span<byte> destination, int location, int length)
    {
        if (destination.Length < length)
        {
            throw new ArgumentException("Destination span is too small.", nameof(destination));
        }

        var remaining = length;
        var chunkIndex = location >> BufferShift;
        var offsetInChunk = location & BufferMask;
        var destOffset = 0;

        while (remaining > 0)
        {
            var available = BufferSize - offsetInChunk;
            var toCopy = Math.Min(remaining, available);

            _chunks[chunkIndex].AsSpan(offsetInChunk, toCopy).CopyTo(destination.Slice(destOffset, toCopy));

            destOffset += toCopy;
            remaining -= toCopy;
            chunkIndex++;
            offsetInChunk = 0;
        }
    }

    /// <summary>
    /// Reads as much data as possible from the current chunk at the given location.
    /// If all requested data fits in one chunk, returns <c>true</c> and <paramref name="span"/>
    /// contains the complete segment. If the data crosses a chunk boundary, returns <c>false</c>,
    /// <paramref name="span"/> contains the portion from the first chunk, and
    /// <paramref name="remaining"/> / <paramref name="nextLocation"/> indicate where to continue.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryRead(
        int location,
        int length,
        out ReadOnlySpan<byte> span,
        out int remaining,
        out int nextLocation)
    {
        var chunkIndex = location >> BufferShift;
        var offsetInChunk = location & BufferMask;
        var available = BufferSize - offsetInChunk;

        if (available >= length)
        {
            span = _chunks[chunkIndex].AsSpan(offsetInChunk, length);
            remaining = 0;
            nextLocation = location + length;
            return true;
        }

        span = _chunks[chunkIndex].AsSpan(offsetInChunk, available);
        remaining = length - available;
        nextLocation = (chunkIndex + 1) << BufferShift;
        return false;
    }

    /// <summary>
    /// Resets the write position to a specific location.
    /// Used for rewinding after a duplicate is detected during dedup.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetTo(int position)
    {
        _currentChunk = position >> BufferShift;
        _currentChunkOffset = position & BufferMask;
        _advanceFromScratch = false;
    }

    /// <summary>
    /// Resets the write position to the beginning.
    /// All rented chunks are kept for reuse.
    /// </summary>
    public void Reset()
    {
        _currentChunk = 0;
        _currentChunkOffset = 0;
        _advanceFromScratch = false;
    }

    /// <summary>
    /// Returns excess chunks beyond the first one.
    /// Call this when the owning store is returned to a pool.
    /// </summary>
    public void Clean()
    {
        for (var i = 1; i < _chunkCount; i++)
        {
            JsonMemory.Return(JsonMemoryKind.Variables, _chunks[i]);
            _chunks[i] = null!;
        }

        _chunkCount = Math.Min(_chunkCount, 1);
        _currentChunk = 0;
        _currentChunkOffset = 0;
        _advanceFromScratch = false;
    }

    /// <summary>
    /// Returns all rented chunks and the chunk array to their pools.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            for (var i = 0; i < _chunkCount; i++)
            {
                JsonMemory.Return(JsonMemoryKind.Variables, _chunks[i]);
                _chunks[i] = null!;
            }

            ArrayPool<byte[]>.Shared.Return(_chunks, clearArray: true);
            _chunkCount = 0;
            _currentChunk = 0;
            _currentChunkOffset = 0;
            _disposed = true;
        }
    }

    private void MoveToNextChunk()
    {
        _currentChunk++;
        _currentChunkOffset = 0;

        if (_currentChunk >= _chunks.Length)
        {
            var newChunks = ArrayPool<byte[]>.Shared.Rent(_chunks.Length * 2);
            Array.Copy(_chunks, newChunks, _chunkCount);
            ArrayPool<byte[]>.Shared.Return(_chunks, clearArray: true);
            _chunks = newChunks;
        }

        if (_currentChunk >= _chunkCount)
        {
            _chunks[_currentChunk] = JsonMemory.Rent(JsonMemoryKind.Variables);
            _chunkCount = _currentChunk + 1;
        }
    }
}

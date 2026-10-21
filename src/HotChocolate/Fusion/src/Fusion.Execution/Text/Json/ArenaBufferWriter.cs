using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> that writes gap-free into geometric memory segments rented
/// from an <see cref="IMemoryArena"/>. The segment schedule matches the data chunk schedule used by
/// <see cref="SourceResultDocument"/>, so the written bytes can be parsed in place via
/// <see cref="SourceResultDocument.ParseFilled(IMemoryArena, MemorySegment[], int, int)"/> without
/// any further copy.
/// </summary>
internal sealed class ArenaBufferWriter : IBufferWriter<byte>, IDisposable
{
    private IMemoryArena? _arena;

    // the first segment, which stays out of a segment table until the writer moves past it
    private MemorySegment _firstSegment;
    private MemorySegment[] _segments;
    private int _rentedChunks;
    private int _currentChunk;
    private int _currentChunkStart;
    private int _currentChunkBytes;
    private int _currentChunkOffset;
    private byte[] _currentBuffer;
    private int _currentBase;
    private byte[] _scratch = [];
    private bool _advanceFromScratch;

    /// <summary>
    /// Creates a writer without an arena. It must be reset with an arena before the first write.
    /// </summary>
    public ArenaBufferWriter()
    {
        _segments = [];
        _currentBuffer = [];
    }

    public ArenaBufferWriter(IMemoryArena arena)
    {
        ArgumentNullException.ThrowIfNull(arena);
        Start(arena);
    }

    /// <summary>
    /// Gets the table of filled segments. Every segment except the last is full; the last holds
    /// <see cref="LastLength"/> bytes.
    /// </summary>
    public MemorySegment[] Segments
    {
        get
        {
            if (_segments.Length == 0)
            {
                RentSegmentTable();
            }

            return _segments;
        }
    }

    /// <summary>
    /// Gets the number of filled segments.
    /// </summary>
    public int UsedChunks => _currentChunk + 1;

    /// <summary>
    /// Gets the number of bytes written into the final segment.
    /// </summary>
    public int LastLength => _currentChunkOffset;

    /// <summary>
    /// Gets the gap-free position of the next byte to write.
    /// </summary>
    public int Position => _currentChunkStart + _currentChunkOffset;

    /// <inheritdoc />
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        var remaining = _currentChunkBytes - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            remaining = _currentChunkBytes;
        }

        // The hint of zero asks for at least one byte; the current chunk always has room because a
        // full chunk rolled over above.
        if (sizeHint <= remaining)
        {
            return _currentBuffer.AsSpan(_currentBase + _currentChunkOffset, remaining);
        }

        // The requested size exceeds the remaining space in this chunk. Hand out a scratch buffer
        // and copy it into the segments gap-free on Advance, so the written layout never has holes.
        if (sizeHint > _scratch.Length)
        {
            ReturnScratch();
            _scratch = ArrayPool<byte>.Shared.Rent(sizeHint);
        }

        _advanceFromScratch = true;
        return _scratch.AsSpan(0, sizeHint);
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        var remaining = _currentChunkBytes - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            remaining = _currentChunkBytes;
        }

        if (sizeHint <= remaining)
        {
            return _currentBuffer.AsMemory(_currentBase + _currentChunkOffset, remaining);
        }

        if (sizeHint > _scratch.Length)
        {
            ReturnScratch();
            _scratch = ArrayPool<byte>.Shared.Rent(sizeHint);
        }

        _advanceFromScratch = true;
        return _scratch.AsMemory(0, sizeHint);
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
            var remaining = _currentChunkBytes - _currentChunkOffset;

            if (remaining == 0)
            {
                MoveToNextChunk();
                remaining = _currentChunkBytes;
            }

            var take = Math.Min(source.Length, remaining);
            source[..take].CopyTo(_currentBuffer.AsSpan(_currentBase + _currentChunkOffset, take));
            _currentChunkOffset += take;
            source = source[take..];
        }
    }

    /// <summary>
    /// Moves the write position back to the start. The rented segments are kept and reused by
    /// later writes.
    /// </summary>
    public void Reset() => ResetTo(0);

    /// <summary>
    /// Moves the write position back to the start and writes into segments rented from
    /// <paramref name="arena"/>. The segments of the previous arena are no longer used.
    /// </summary>
    public void Reset(IMemoryArena arena)
    {
        ArgumentNullException.ThrowIfNull(arena);
        Start(arena);
    }

    /// <summary>
    /// Drops the segments of the arena. The writer must be reset with an arena before the next write.
    /// </summary>
    public void Release()
    {
        _arena = null;
        _firstSegment = default;
        _segments = [];
        _currentBuffer = [];
        _rentedChunks = 0;
        _currentChunk = 0;
        _currentChunkStart = 0;
        _currentChunkBytes = 0;
        _currentChunkOffset = 0;
        _currentBase = 0;
        _advanceFromScratch = false;
    }

    /// <summary>
    /// Moves the write position back to <paramref name="position"/>, which must not be after
    /// <see cref="Position"/>. Later writes overwrite the bytes after it.
    /// </summary>
    public void ResetTo(int position)
    {
        Debug.Assert(
            position >= 0 && position <= Position,
            "The position must not be after the write position.");

        while (position < _currentChunkStart)
        {
            _currentChunk--;
            _currentChunkStart -= GetSegment(_currentChunk).Length;
        }

        var segment = GetSegment(_currentChunk);
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
        _currentChunkBytes = segment.Length;
        _currentChunkOffset = position - _currentChunkStart;
        _advanceFromScratch = false;
    }

    /// <summary>
    /// Compares <paramref name="length"/> written bytes at <paramref name="positionA"/> with the
    /// written bytes at <paramref name="positionB"/>.
    /// </summary>
    public bool SequenceEqual(int positionA, int positionB, int length)
    {
        if (positionA == positionB || length == 0)
        {
            return true;
        }

        var chunkA = FindChunk(positionA, out var offsetA);
        var chunkB = FindChunk(positionB, out var offsetB);

        while (true)
        {
            var segmentA = GetSegment(chunkA);
            var segmentB = GetSegment(chunkB);
            var count = Math.Min(length, Math.Min(segmentA.Length - offsetA, segmentB.Length - offsetB));

            if (!segmentA.Buffer.AsSpan(segmentA.Offset + offsetA, count).SequenceEqual(
                segmentB.Buffer.AsSpan(segmentB.Offset + offsetB, count)))
            {
                return false;
            }

            length -= count;

            if (length == 0)
            {
                return true;
            }

            offsetA += count;
            offsetB += count;

            if (offsetA == segmentA.Length)
            {
                chunkA++;
                offsetA = 0;
            }

            if (offsetB == segmentB.Length)
            {
                chunkB++;
                offsetB = 0;
            }
        }
    }

    /// <summary>
    /// Reads at most <paramref name="length"/> written bytes at <paramref name="start"/> from one
    /// segment and moves <paramref name="start"/> and <paramref name="length"/> past them.
    /// </summary>
    public ReadOnlySpan<byte> Read(ref int start, ref int length)
    {
        if (length == 0)
        {
            return default;
        }

        var segment = GetSegment(FindChunk(start, out var offset));
        var count = Math.Min(length, segment.Length - offset);
        var bytes = segment.Buffer.AsSpan(segment.Offset + offset, count);
        start += count;
        length -= count;
        return bytes;
    }

    /// <summary>
    /// Reads at most <paramref name="length"/> written bytes at <paramref name="start"/> from one
    /// segment and moves <paramref name="start"/> and <paramref name="length"/> past them.
    /// </summary>
    public ReadOnlyMemory<byte> ReadMemory(ref int start, ref int length)
    {
        if (length == 0)
        {
            return default;
        }

        var segment = GetSegment(FindChunk(start, out var offset));
        var count = Math.Min(length, segment.Length - offset);
        var bytes = segment.Buffer.AsMemory(segment.Offset + offset, count);
        start += count;
        length -= count;
        return bytes;
    }

    /// <summary>
    /// Computes the hash code of <paramref name="length"/> written bytes at
    /// <paramref name="location"/>. Equal bytes have equal hash codes.
    /// </summary>
    public int GetHashCode(int location, int length)
    {
        var hash = 0u;

        while (length > 0)
        {
            hash = ChunkedArrayWriter.ComputeHashCore(hash, Read(ref location, ref length));
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    public void Dispose()
    {
        ReturnScratch();
    }

    [MemberNotNull(nameof(_arena), nameof(_segments), nameof(_currentBuffer))]
    private void Start(IMemoryArena arena)
    {
        _arena = arena;

        // Rent the first segment now so the first write does not have to branch on an empty writer.
        // The segment table is rented once the writer moves past the first segment.
        _currentChunkBytes = SourceResultDocument.GetDataChunkSize(0);
        var segment = arena.Rent(_currentChunkBytes);
        _firstSegment = segment;
        _segments = [];
        _rentedChunks = 1;
        _currentChunk = 0;
        _currentChunkStart = 0;
        _currentChunkOffset = 0;
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
        _advanceFromScratch = false;
    }

    private int FindChunk(int position, out int offset)
    {
        var chunk = 0;
        var length = _firstSegment.Length;

        while (position >= length)
        {
            position -= length;
            length = _segments[++chunk].Length;
        }

        offset = position;
        return chunk;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private MemorySegment GetSegment(int chunk)
        => chunk == 0 ? _firstSegment : _segments[chunk];

    private void RentSegmentTable()
    {
        Debug.Assert(_arena is not null, "The writer must be reset with an arena before writing.");

        _segments = _arena.RentSegmentTable(16);
        _segments[0] = _firstSegment;
    }

    private void ReturnScratch()
    {
        if (_scratch.Length > 0)
        {
            ArrayPool<byte>.Shared.Return(_scratch);
            _scratch = [];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveToNextChunk()
    {
        _currentChunkStart += _currentChunkBytes;
        _currentChunk++;

        if (_currentChunk >= _rentedChunks)
        {
            Debug.Assert(_arena is not null, "The writer must be reset with an arena before writing.");

            if (_currentChunk >= SourceResultDocument.DataMaxChunks)
            {
                throw new InvalidOperationException(
                    "The source result document has exceeded its maximum data capacity.");
            }

            if (_segments.Length == 0)
            {
                RentSegmentTable();
            }
            else if (_currentChunk >= _segments.Length)
            {
                _arena.GrowSegmentTable(ref _segments);
            }

            _segments[_currentChunk] = _arena.Rent(SourceResultDocument.GetDataChunkSize(_currentChunk));
            _rentedChunks = _currentChunk + 1;
        }

        var segment = _segments[_currentChunk];
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
        _currentChunkBytes = segment.Length;
        _currentChunkOffset = 0;
    }
}

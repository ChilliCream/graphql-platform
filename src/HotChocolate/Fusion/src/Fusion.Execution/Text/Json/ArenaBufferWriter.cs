using System.Buffers;
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
internal sealed class ArenaBufferWriter : IBufferWriter<byte>
{
    private readonly IMemoryArena _arena;
    private MemorySegment[] _segments;
    private int _usedChunks;
    private int _currentChunk;
    private int _currentChunkBytes;
    private int _currentChunkOffset;
    private byte[] _currentBuffer;
    private int _currentBase;
    private byte[] _scratch = [];
    private bool _advanceFromScratch;

    public ArenaBufferWriter(IMemoryArena arena)
    {
        ArgumentNullException.ThrowIfNull(arena);
        _arena = arena;

        // Rent the first segment now so the first write does not have to branch on an empty table.
        _segments = arena.RentSegmentTable(64);
        _currentChunkBytes = SourceResultDocument.GetDataChunkSize(0);
        var segment = arena.Rent(_currentChunkBytes);
        _segments[0] = segment;
        _usedChunks = 1;
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
    }

    /// <summary>
    /// Gets the table of filled segments. Every segment except the last is full; the last holds
    /// <see cref="LastLength"/> bytes.
    /// </summary>
    public MemorySegment[] Segments => _segments;

    /// <summary>
    /// Gets the number of filled segments.
    /// </summary>
    public int UsedChunks => _usedChunks;

    /// <summary>
    /// Gets the number of bytes written into the final segment.
    /// </summary>
    public int LastLength => _currentChunkOffset;

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
            return _currentBuffer.AsSpan(_currentBase + _currentChunkOffset);
        }

        // The requested size exceeds the remaining space in this chunk. Hand out a scratch buffer
        // and copy it into the segments gap-free on Advance, so the written layout never has holes.
        if (sizeHint > _scratch.Length)
        {
            _scratch = new byte[sizeHint];
        }

        _advanceFromScratch = true;
        return _scratch;
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
            _scratch = new byte[sizeHint];
        }

        _advanceFromScratch = true;
        return _scratch.AsMemory();
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MoveToNextChunk()
    {
        _currentChunk++;

        if (_currentChunk >= _usedChunks)
        {
            if (_currentChunk >= SourceResultDocument.DataMaxChunks)
            {
                throw new InvalidOperationException(
                    "The source result document has exceeded its maximum data capacity.");
            }

            if (_currentChunk >= _segments.Length)
            {
                _arena.GrowSegmentTable(ref _segments);
            }

            _segments[_currentChunk] = _arena.Rent(SourceResultDocument.GetDataChunkSize(_currentChunk));
            _usedChunks = _currentChunk + 1;
        }

        var segment = _segments[_currentChunk];
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
        _currentChunkBytes = segment.Length;
        _currentChunkOffset = 0;
    }
}

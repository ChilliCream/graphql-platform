using System.Buffers;
using System.Runtime.CompilerServices;
#if NET8_0_OR_GREATER
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
#endif
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> that writes gap-free into geometric memory segments rented
/// from an <see cref="IMemoryArena"/>. The segment schedule matches the data chunk schedule used by
/// <see cref="SourceResultDocument"/>, so the written bytes can be parsed in place via
/// <see cref="SourceResultDocument.ParseFilled(IMemoryArena, MemorySegment[], int, int)"/> without
/// any further copy.
/// </summary>
internal sealed class ArenaBufferWriter : IBufferWriter<byte>, IJsonSegmentSource, IDisposable
{
    private const int DataOffsetBits = 17;
    private const int DataOffsetMask = (1 << DataOffsetBits) - 1;
    private const int DefaultScratchSize = 128;
    private const int SimdThreshold = 64;

    private readonly IMemoryArena _arena;
    private MemorySegment[] _segments;
    private int _usedChunks;
    private int _currentChunk;
    private int _currentChunkBytes;
    private int _currentChunkOffset;
    private int _position;
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
    /// Gets the current gap-free write position.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the total number of bytes written.
    /// </summary>
    public int Length => _position;

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
        var size = sizeHint < 1 ? DefaultScratchSize : sizeHint;
        var remaining = _currentChunkBytes - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            remaining = _currentChunkBytes;
        }

        // The hint of zero asks for at least one byte; the current chunk always has room because a
        // full chunk rolled over above.
        if (size <= remaining)
        {
            return _currentBuffer.AsSpan(_currentBase + _currentChunkOffset, remaining);
        }

        // The requested size exceeds the remaining space in this chunk. Hand out a scratch buffer
        // and copy it into the segments gap-free on Advance, so the written layout never has holes.
        if (size > _scratch.Length)
        {
            ReturnScratch();
            _scratch = ArrayPool<byte>.Shared.Rent(size);
        }

        _advanceFromScratch = true;
        return _scratch.AsSpan(0, size);
    }

    /// <inheritdoc />
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        var size = sizeHint < 1 ? DefaultScratchSize : sizeHint;
        var remaining = _currentChunkBytes - _currentChunkOffset;
        _advanceFromScratch = false;

        if (remaining == 0)
        {
            MoveToNextChunk();
            remaining = _currentChunkBytes;
        }

        if (size <= remaining)
        {
            return _currentBuffer.AsMemory(_currentBase + _currentChunkOffset, remaining);
        }

        if (size > _scratch.Length)
        {
            ReturnScratch();
            _scratch = ArrayPool<byte>.Shared.Rent(size);
        }

        _advanceFromScratch = true;
        return _scratch.AsMemory(0, size);
    }

    /// <inheritdoc />
    public void Advance(int count)
    {
        if (!_advanceFromScratch)
        {
            _currentChunkOffset += count;
            _position += count;
            return;
        }

        _advanceFromScratch = false;
        var advanced = count;
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

        _position += advanced;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> Read(ref int start, ref int length)
    {
        GetChunkOffset(start, out var chunkIndex, out var offsetInChunk);
        var available = SourceResultDocument.GetDataChunkSize(chunkIndex) - offsetInChunk;

        if (available >= length)
        {
            var span = SegmentSpan(chunkIndex, offsetInChunk, length);
            length = 0;
            return span;
        }

        start += available;
        length -= available;
        return SegmentSpan(chunkIndex, offsetInChunk, available);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SequenceEqual(int locationA, int locationB, int length)
    {
        if (locationA == locationB || length == 0)
        {
            return true;
        }

        GetChunkOffset(locationA, out var chunkA, out var offsetA);
        GetChunkOffset(locationB, out var chunkB, out var offsetB);

        var availA = SourceResultDocument.GetDataChunkSize(chunkA) - offsetA;
        var availB = SourceResultDocument.GetDataChunkSize(chunkB) - offsetB;

        if (availA >= length && availB >= length)
        {
            return SegmentSpan(chunkA, offsetA, length).SequenceEqual(
                SegmentSpan(chunkB, offsetB, length));
        }

        return SequenceEqualMultiChunk(chunkA, offsetA, chunkB, offsetB, length);
    }

    private bool SequenceEqualMultiChunk(
        int chunkA,
        int offsetA,
        int chunkB,
        int offsetB,
        int remaining)
    {
        while (remaining > 0)
        {
            var availA = SourceResultDocument.GetDataChunkSize(chunkA) - offsetA;
            var availB = SourceResultDocument.GetDataChunkSize(chunkB) - offsetB;
            var toCompare = Math.Min(remaining, Math.Min(availA, availB));

            if (!SegmentSpan(chunkA, offsetA, toCompare).SequenceEqual(
                    SegmentSpan(chunkB, offsetB, toCompare)))
            {
                return false;
            }

            remaining -= toCompare;
            offsetA += toCompare;
            offsetB += toCompare;

            if (offsetA >= SourceResultDocument.GetDataChunkSize(chunkA))
            {
                chunkA++;
                offsetA = 0;
            }

            if (offsetB >= SourceResultDocument.GetDataChunkSize(chunkB))
            {
                chunkB++;
                offsetB = 0;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetHashCode(int location, int length)
    {
        if (length == 0)
        {
            return 0;
        }

        GetChunkOffset(location, out var chunkIndex, out var offsetInChunk);
        var availableInChunk = SourceResultDocument.GetDataChunkSize(chunkIndex) - offsetInChunk;

        if (availableInChunk >= length)
        {
            return (int)(ComputeHashCore(0u, SegmentSpan(chunkIndex, offsetInChunk, length)) & 0x7FFFFFFF);
        }

        var hash = 0u;

        while (length > 0)
        {
            var toHash = Math.Min(length, SourceResultDocument.GetDataChunkSize(chunkIndex) - offsetInChunk);
            hash = ComputeHashCore(hash, SegmentSpan(chunkIndex, offsetInChunk, toHash));
            length -= toHash;
            chunkIndex++;
            offsetInChunk = 0;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ResetTo(int position)
    {
        GetChunkOffset(position, out _currentChunk, out _currentChunkOffset);
        var segment = _segments[_currentChunk];
        _currentBuffer = segment.Buffer;
        _currentBase = segment.Offset;
        _currentChunkBytes = segment.Length;
        _position = position;
        _advanceFromScratch = false;
    }

    public void Reset()
    {
        ResetTo(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ComputeHashCore(uint hash, ReadOnlySpan<byte> bytes)
    {
#if NET8_0_OR_GREATER
        if (bytes.Length >= SimdThreshold)
        {
            return ComputeHashSimd(hash, bytes);
        }
#endif
        unchecked
        {
            foreach (var b in bytes)
            {
                hash = (hash * 31) + b;
            }
        }

        return hash;
    }

#if NET8_0_OR_GREATER
    private static uint ComputeHashSimd(uint hash, ReadOnlySpan<byte> bytes)
    {
        unchecked
        {
            const uint pow31_1 = 31;
            const uint pow31_2 = 31 * 31;
            const uint pow31_3 = 31 * 31 * 31;
            const uint pow31_4 = 31 * 31 * 31 * 31;
            const uint pow31_5 = pow31_4 * 31;
            const uint pow31_6 = pow31_5 * 31;
            const uint pow31_7 = pow31_6 * 31;
            const uint pow31_8 = pow31_7 * 31;

            ref var src = ref MemoryMarshal.GetReference(bytes);
            var i = 0;

            if (Vector256.IsHardwareAccelerated && bytes.Length >= 64)
            {
                var acc = Vector256<uint>.Zero;
                var mul = Vector256.Create(pow31_8);
                var simdEnd = bytes.Length & ~7;

                for (; i < simdEnd; i += 8)
                {
                    var raw = Vector128.CreateScalarUnsafe(
                        Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref src, i))).AsByte();
                    var (loShort, _) = Vector128.Widen(raw);
                    var (lo32, hi32) = Vector128.Widen(loShort);
                    var wide = Vector256.Create(lo32, hi32);

                    acc = (acc * mul) + wide;
                }

                var finalPow = Vector256.Create(
                    pow31_7, pow31_6, pow31_5, pow31_4,
                    pow31_3, pow31_2, pow31_1, 1u);
                acc *= finalPow;

                var sum128 = acc.GetLower() + acc.GetUpper();
                var t = sum128 + Vector128.Shuffle(sum128, Vector128.Create(2u, 3u, 0u, 1u));
                var simdResult = (t + Vector128.Shuffle(t, Vector128.Create(1u, 0u, 3u, 2u)))
                    .ToScalar();

                hash = (hash * Pow31(simdEnd)) + simdResult;
            }

            if (Vector128.IsHardwareAccelerated && bytes.Length - i >= 4)
            {
                var acc = Vector128<uint>.Zero;
                var mul = Vector128.Create(pow31_4);
                var simdEnd = i + ((bytes.Length - i) & ~3);
                var simdStart = i;

                for (; i < simdEnd; i += 4)
                {
                    var raw = Vector128.CreateScalarUnsafe(
                        Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref src, i))).AsByte();
                    var (loShort, _) = Vector128.Widen(raw);
                    var (wide, _) = Vector128.Widen(loShort);

                    acc = (acc * mul) + wide;
                }

                var finalPow = Vector128.Create(pow31_3, pow31_2, pow31_1, 1u);
                acc *= finalPow;

                var t = acc + Vector128.Shuffle(acc, Vector128.Create(2u, 3u, 0u, 1u));
                var simdResult = (t + Vector128.Shuffle(t, Vector128.Create(1u, 0u, 3u, 2u)))
                    .ToScalar();

                hash = (hash * Pow31(simdEnd - simdStart)) + simdResult;
            }

            for (; i < bytes.Length; i++)
            {
                hash = (hash * 31) + Unsafe.Add(ref src, i);
            }

            return hash;
        }
    }

    private static uint Pow31(int n)
    {
        unchecked
        {
            var result = 1u;
            var b = 31u;

            while (n > 0)
            {
                if ((n & 1) != 0)
                {
                    result *= b;
                }

                b *= b;
                n >>= 1;
            }

            return result;
        }
    }
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> SegmentSpan(int chunkIndex, int offsetInChunk, int length)
    {
        var segment = _segments[chunkIndex];
        return segment.Buffer.AsSpan(segment.Offset + offsetInChunk, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void GetChunkOffset(int position, out int chunkIndex, out int offsetInChunk)
    {
        var packed = SourceResultDocument.LinearToPacked(position);
        chunkIndex = packed >>> DataOffsetBits;
        offsetInChunk = packed & DataOffsetMask;
    }

    public void Dispose()
    {
        ReturnScratch();
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

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class SourceResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _parsedData;
    private readonly MemorySegment[] _segments;
    private readonly int _usedChunks;
    private int _disposed;

    private SourceResultDocument(MetaDb parsedData, MemorySegment[] segments, int usedChunks)
    {
        _parsedData = parsedData;
        _segments = segments;
        _usedChunks = usedChunks;
        Root = new SourceResultElement(this, Cursor.Zero);
    }

    internal int Id { get; set; } = -1;

    public SourceResultElement Root { get; private set; }

    // A data location packs the chunk index in the high 12 bits and the byte offset within the
    // chunk in the low 17 bits. The chunk size follows the same geometric schedule as the metadb,
    // so it is derived from the chunk index (chunk i holds 1 << (10 + Min(i, 7)) bytes). The data
    // store is written without gaps; a value that does not fit the remaining space of a chunk spans
    // into the following chunks, and the read path walks the boundaries.
    private const int DataOffsetBits = 17;
    private const int DataChunkBits = 12;
    private const int DataOffsetMask = (1 << DataOffsetBits) - 1;
    internal const int DataMaxChunks = 1 << DataChunkBits;
    private const int DataMaxChunkOrdinal = (int)ChunkSize.Size128K;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int GetDataChunkSize(int chunkIndex)
        => 1 << (10 + Math.Min(chunkIndex, DataMaxChunkOrdinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int EncodeDataLocation(int chunkIndex, int offset)
        => (chunkIndex << DataOffsetBits) | offset;

    // The cumulative number of bytes held by all data chunks before a given chunk index, following
    // the same geometric schedule as the chunks themselves. The ramp (chunks 0..7) is a small
    // prefix table; from chunk 8 on every chunk holds the same number of bytes, so the tail is
    // closed-form. This lets a packed location be mapped to the gap-free linear byte position it
    // represents, which the composite-value readers need to measure the span of an object or array.
    private static readonly int[] s_dataRampPrefix = BuildDataRampPrefix();

    private static int[] BuildDataRampPrefix()
    {
        var prefix = new int[DataMaxChunkOrdinal + 1];
        var cumulative = 0;

        for (var chunk = 0; chunk < DataMaxChunkOrdinal; chunk++)
        {
            prefix[chunk] = cumulative;
            cumulative += GetDataChunkSize(chunk);
        }

        prefix[DataMaxChunkOrdinal] = cumulative;
        return prefix;
    }

    /// <summary>
    /// Maps a gap-free linear byte position to the packed data location that represents it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int LinearToPacked(long linear)
    {
        // Walk the geometric ramp, then close-form the constant-size tail.
        for (var chunk = 0; chunk < DataMaxChunkOrdinal; chunk++)
        {
            var chunkBytes = GetDataChunkSize(chunk);

            if (linear < chunkBytes)
            {
                return EncodeDataLocation(chunk, (int)linear);
            }

            linear -= chunkBytes;
        }

        var maxChunkBytes = GetDataChunkSize(DataMaxChunkOrdinal);
        var tailChunk = DataMaxChunkOrdinal + (int)(linear / maxChunkBytes);
        var tailOffset = (int)(linear % maxChunkBytes);
        return EncodeDataLocation(tailChunk, tailOffset);
    }

    /// <summary>
    /// Maps a packed data location to the gap-free linear byte position it represents.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long PackedToLinear(int location)
    {
        var chunkIndex = location >>> DataOffsetBits;
        var offset = location & DataOffsetMask;

        var bytesBefore = chunkIndex <= DataMaxChunkOrdinal
            ? s_dataRampPrefix[chunkIndex]
            : s_dataRampPrefix[DataMaxChunkOrdinal]
                + ((long)(chunkIndex - DataMaxChunkOrdinal) * GetDataChunkSize(DataMaxChunkOrdinal));

        return bytesBefore + offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal JsonTokenType GetElementTokenType(Cursor cursor)
        => _parsedData.GetJsonTokenType(cursor);

    internal int GetArrayLength(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(cursor);

        CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

        return row.SizeOrLength;
    }

    internal int GetPropertyCount(Cursor cursor)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(cursor);

        CheckExpectedType(JsonTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    internal SourceResultElement GetArrayIndexElement(Cursor startCursor, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _parsedData.Get(startCursor);

        CheckExpectedType(JsonTokenType.StartArray, row.TokenType);

        var arrayLength = row.SizeOrLength;

        if ((uint)arrayIndex >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        if (!row.HasComplexChildren)
        {
            // Since we wouldn't be here without having completed the document parse, and we
            // already vetted the index against the length, this new index will always be
            // within the table.
            var target = startCursor + (arrayIndex + 1);
            return new SourceResultElement(this, target);
        }

        var elementCount = 0;
        var cursor = startCursor + 1;

        while (true)
        {
            if (elementCount == arrayIndex)
            {
                return new SourceResultElement(this, cursor);
            }

            var child = _parsedData.Get(cursor);

            if (child.IsSimpleValue)
            {
                cursor++;
            }
            else
            {
                cursor += child.NumberOfRows;
            }

            elementCount++;
        }
    }

    internal void WriteRawValueTo(IBufferWriter<byte> writer, int location, int size)
    {
        var chunkIndex = location >>> DataOffsetBits;
        var offsetInChunk = location & DataOffsetMask;
        var bytesRead = 0;

        var seg = _segments[chunkIndex];

        // Fast path: data fits in a single chunk
        if (offsetInChunk + size <= seg.Length)
        {
            var single = writer.GetSpan(size);
            seg.Buffer.AsSpan(seg.Offset + offsetInChunk, size).CopyTo(single);
            writer.Advance(size);
            return;
        }

        while (bytesRead < size)
        {
            seg = _segments[chunkIndex];
            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, seg.Length - offsetInChunk);
            var chunkSpan = seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToCopyFromThisChunk);

            var span = writer.GetSpan(chunkSpan.Length);
            chunkSpan.CopyTo(span);
            writer.Advance(chunkSpan.Length);
            bytesRead += bytesToCopyFromThisChunk;
            chunkIndex++;
            offsetInChunk = 0;
        }
    }

    internal void WriteRawStringValueTo(JsonWriter writer, int location, int size)
    {
        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        Debug.Assert((uint)startChunkIndex < (uint)_segments.Length);
        ref readonly var startSeg = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_segments),
            startChunkIndex);

        // Fast path: the value lives in a single chunk and can be written without a copy.
        if (offsetInStartChunk + size <= startSeg.Length)
        {
            ref var start = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(startSeg.Buffer),
                startSeg.Offset + offsetInStartChunk);
            writer.WriteStringValue(
                MemoryMarshal.CreateReadOnlySpan(ref start, size),
                skipEscaping: true);
            return;
        }

        WriteCrossChunkValueTo(
            writer,
            startChunkIndex,
            offsetInStartChunk,
            size,
            JsonTokenType.String);
    }

    internal void WriteRawNumberValueTo(JsonWriter writer, int location, int size)
    {
        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        ref readonly var startSeg = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_segments),
            startChunkIndex);

        // Fast path: the value lives in a single chunk and can be written without a copy.
        if (offsetInStartChunk + size <= startSeg.Length)
        {
            ref var start = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(startSeg.Buffer),
                startSeg.Offset + offsetInStartChunk);
            writer.WriteNumberValue(MemoryMarshal.CreateReadOnlySpan(ref start, size));
            return;
        }

        WriteCrossChunkValueTo(
            writer,
            startChunkIndex,
            offsetInStartChunk,
            size,
            JsonTokenType.Number);
    }

    private void WriteCrossChunkValueTo(
        JsonWriter writer,
        int startChunkIndex,
        int offsetInStartChunk,
        int size,
        JsonTokenType tokenType)
    {
        Debug.Assert(tokenType is JsonTokenType.String or JsonTokenType.Number);

        if (size > JsonConstants.MaxUnescapedTokenSize)
        {
            ThrowHelper.ThrowArgumentException_ValueTooLarge(size);
        }

        if (writer.Options.Indented)
        {
            var scratch = ArrayPool<byte>.Shared.Rent(size);

            try
            {
                GatherRawValue(scratch, startChunkIndex, offsetInStartChunk, size);

                if (tokenType is JsonTokenType.String)
                {
                    writer.WriteStringValue(scratch.AsSpan(0, size), skipEscaping: true);
                }
                else
                {
                    writer.WriteNumberValue(scratch.AsSpan(0, size));
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(scratch);
            }

            return;
        }

        var bytesRead = 0;
        var chunkIndex = startChunkIndex;
        var offsetInChunk = offsetInStartChunk;

        while (bytesRead < size)
        {
            var seg = _segments[chunkIndex];
            var bytesToWrite = Math.Min(size - bytesRead, seg.Length - offsetInChunk);
            var chunkSpan = seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToWrite);

            if (bytesRead == 0)
            {
                writer.WriteRawValueStart(chunkSpan);
            }
            else
            {
                writer.WriteRawValueContinuation(chunkSpan);
            }

            bytesRead += bytesToWrite;
            chunkIndex++;
            offsetInChunk = 0;
        }

        writer.WriteRawValueEnd(tokenType);
    }

    private void GatherRawValue(byte[] destination, int startChunkIndex, int offsetInStartChunk, int size)
    {
        var bytesRead = 0;
        var chunkIndex = startChunkIndex;
        var offsetInChunk = offsetInStartChunk;

        while (bytesRead < size)
        {
            var seg = _segments[chunkIndex];
            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, seg.Length - offsetInChunk);
            var chunkSpan = seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToCopyFromThisChunk);

            chunkSpan.CopyTo(destination.AsSpan(bytesRead));
            bytesRead += bytesToCopyFromThisChunk;
            chunkIndex++;
            offsetInChunk = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadRawValue(DbRow row, bool includeQuotes)
    {
        // Strings and property names are stored quote-inclusive: the location points at the opening
        // quote and the size covers both quotes. The unquoted value is obtained by slicing one byte
        // in on each side, which keeps every stored location pointing at a real byte and avoids any
        // arithmetic on the packed location.
        if (!includeQuotes && row.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
        {
            return ReadRawValue(row.Location, row.SizeOrLength)[1..^1];
        }

        return ReadRawValue(row.Location, row.SizeOrLength);
    }

    /// <summary>
    /// Reads raw data from the data chunks. Data contained in a single chunk is returned as a
    /// slice over that chunk; data that spans chunk boundaries is copied into a fresh buffer.
    /// </summary>
    internal ReadOnlySpan<byte> ReadRawValue(int location, int size)
    {
        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        ref readonly var startSeg = ref Unsafe.Add(
            ref MemoryMarshal.GetArrayDataReference(_segments),
            startChunkIndex);

        // Fast path: data fits in a single chunk
        if (offsetInStartChunk + size <= startSeg.Length)
        {
            ref var start = ref Unsafe.Add(
                ref MemoryMarshal.GetArrayDataReference(startSeg.Buffer),
                startSeg.Offset + offsetInStartChunk);
            return MemoryMarshal.CreateReadOnlySpan(ref start, size);
        }

        Span<byte> buffer = new byte[size];
        var bytesRead = 0;
        var chunkIndex = startChunkIndex;
        var offsetInChunk = offsetInStartChunk;

        while (bytesRead < size)
        {
            var seg = _segments[chunkIndex];
            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, seg.Length - offsetInChunk);
            var chunkSpan = seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToCopyFromThisChunk);

            chunkSpan.CopyTo(buffer[bytesRead..]);
            bytesRead += bytesToCopyFromThisChunk;
            chunkIndex++;
            offsetInChunk = 0;
        }

        return buffer;
    }

    internal ReadOnlyMemory<byte> ReadRawValueAsMemory(int location, int size)
    {
        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        var startSeg = _segments[startChunkIndex];

        // Fast path: data fits in a single chunk
        if (offsetInStartChunk + size <= startSeg.Length)
        {
            return startSeg.Buffer.AsMemory(startSeg.Offset + offsetInStartChunk, size);
        }

        var tempArray = new byte[size];
        var bytesRead = 0;
        var chunkIndex = startChunkIndex;
        var offsetInChunk = offsetInStartChunk;

        while (bytesRead < size)
        {
            var seg = _segments[chunkIndex];
            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, seg.Length - offsetInChunk);

            seg.Buffer.AsSpan(seg.Offset + offsetInChunk, bytesToCopyFromThisChunk)
                .CopyTo(tempArray.AsSpan(bytesRead));

            bytesRead += bytesToCopyFromThisChunk;
            chunkIndex++;
            offsetInChunk = 0;
        }

        return tempArray;
    }

    private static void CheckExpectedType(JsonTokenType expected, JsonTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but got {actual}.");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _parsedData.Dispose();
    }

    public override string ToString()
    {
        if (_usedChunks == 0)
        {
            return string.Empty;
        }

        var totalSize = 0;

        for (var i = 0; i < _usedChunks; i++)
        {
            totalSize += _segments[i].Length;
        }

        var buffer = new byte[totalSize];
        var offset = 0;

        for (var i = 0; i < _usedChunks; i++)
        {
            var seg = _segments[i];
            seg.Buffer.AsSpan(seg.Offset, seg.Length).CopyTo(buffer.AsSpan(offset));
            offset += seg.Length;
        }

        return s_utf8Encoding.GetString(buffer).TrimEnd('\0');
    }
}

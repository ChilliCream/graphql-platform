using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal sealed partial class SourceResultDocumentBuilder : IDisposable
{
    private readonly Operation _operation;
    private readonly IMemoryArena _arena;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal readonly PooledArrayWriter _data = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal MetaDb _metaDb;

    private bool _disposed;

    public SourceResultDocumentBuilder(
        IMemoryArena arena,
        Operation operation,
        ulong includeFlags,
        SelectionSet? selectionSet = null)
    {
        _arena = arena ?? throw new ArgumentNullException(nameof(arena));
        _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        _metaDb = new MetaDb();

        selectionSet ??= operation.RootSelectionSet;
        var rootIndex = CreateObjectValue(selectionSet.Selections, includeFlags);
        Root = new SourceResultElementBuilder(this, rootIndex);
    }

    public SourceResultElementBuilder Root { get; }

    internal ElementTokenType GetElementTokenType(int index)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return _metaDb.GetElementTokenType(index);
    }

    internal int GetStartIndex(int index)
    {
        if (_metaDb.GetElementTokenType(index) is ElementTokenType.Reference)
        {
            return _metaDb.GetLocation(index);
        }

        return index;
    }

    internal int GetEndIndex(int index) => index + _metaDb.GetNumberOfRows(index) - 1;

    internal int CreateObjectValue(ReadOnlySpan<Selection> selections, ulong includeFlags)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var propertyCount = 0;
        var start = _metaDb.Append(ElementTokenType.StartObject);

        foreach (var selection in selections)
        {
            if (!selection.IsIncluded(includeFlags))
            {
                continue;
            }

            propertyCount++;
            _metaDb.Append(ElementTokenType.PropertyName);
            _metaDb.Append(ElementTokenType.None);
        }

        var rows = (propertyCount * 2) + 2;

        _metaDb.Append(ElementTokenType.EndObject, sizeOrLength: propertyCount, rows: rows);
        _metaDb.SetRows(start, rows);
        _metaDb.SetSizeOrLength(start, propertyCount);

        return start;
    }

    public int CreateListValue(int length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var start = _metaDb.Append(ElementTokenType.StartArray, sizeOrLength: length, rows: length + 2);

        for (var i = 0; i < length; i++)
        {
            _metaDb.Append(ElementTokenType.None);
        }

        _metaDb.Append(ElementTokenType.EndArray, sizeOrLength: length, rows: length + 2);

        return start;
    }

    internal void AssignReference(SourceResultElementBuilder element, SourceResultElementBuilder value)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Debug.Assert(value.ValueKind is JsonValueKind.Object or JsonValueKind.Array);
        _metaDb.SetElementTokenType(element._index, ElementTokenType.Reference);
        _metaDb.SetLocation(element._index, value._index);
    }

    public SourceResultDocument Build()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Write the JSON directly into the document's own gap-free arena segments, then parse those
        // bytes in place without any intermediate staging buffer or extra copy. The arena owns both
        // the chunk memory and the segment table.
        var segments = _arena.RentSegmentTable(64);
        segments[0] = _arena.Rent(SourceResultDocument.GetDataChunkSize(0));

        var currentChunkIndex = 0;
        var currentChunkOffset = 0;

        WriteElement(0, ref segments, ref currentChunkIndex, ref currentChunkOffset);

        var usedChunks = currentChunkIndex + 1;
        var lastLength = currentChunkOffset;

        return SourceResultDocument.ParseFilled(_arena, segments, usedChunks, lastLength);
    }

    private void WriteElement(
        int index,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        switch (_metaDb.GetElementTokenType(index))
        {
            case ElementTokenType.StartObject:
                WriteObject(index, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.StartArray:
                WriteArray(index, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.String:
                WriteString(index, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.Number:
                WriteNumber(index, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.True:
                WriteBytes("true"u8, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.False:
                WriteBytes("false"u8, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.None:
            case ElementTokenType.Null:
                WriteBytes("null"u8, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.Reference:
                var referenceIndex = _metaDb.GetLocation(index);
                WriteElement(referenceIndex, ref segments, ref currentChunkIndex, ref currentChunkOffset);
                break;
        }
    }

    private void WriteObject(
        int startIndex,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        WriteByte((byte)'{', ref segments, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'\n', ref segments, ref currentChunkIndex, ref currentChunkOffset);

        startIndex = GetStartIndex(startIndex);
        var current = startIndex;
        var endIndex = GetEndIndex(startIndex) - 1;

        while (current < endIndex)
        {
            if (current > startIndex)
            {
                WriteByte((byte)',', ref segments, ref currentChunkIndex, ref currentChunkOffset);
            }

            WritePropertyName(++current, ref segments, ref currentChunkIndex, ref currentChunkOffset);
            WriteByte((byte)':', ref segments, ref currentChunkIndex, ref currentChunkOffset);
            WriteElement(++current, ref segments, ref currentChunkIndex, ref currentChunkOffset);
            WriteByte((byte)'\n', ref segments, ref currentChunkIndex, ref currentChunkOffset);
        }

        WriteByte((byte)'}', ref segments, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'\n', ref segments, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteArray(
        int startIndex,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        WriteByte((byte)'[', ref segments, ref currentChunkIndex, ref currentChunkOffset);

        var row = _metaDb.Get(startIndex);
        var elementCount = row.SizeOrLength;
        var elementIndex = startIndex + 1;

        for (var i = 0; i < elementCount; i++)
        {
            if (i > 0)
            {
                WriteByte((byte)',', ref segments, ref currentChunkIndex, ref currentChunkOffset);
            }

            WriteElement(elementIndex + i, ref segments, ref currentChunkIndex, ref currentChunkOffset);
        }

        WriteByte((byte)']', ref segments, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WritePropertyName(
        int index,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var selectionId = row.Location;

        // Get the selection to retrieve the UTF-8 response name
        var selection = _operation.GetSelectionById(selectionId);
        var nameBytes = selection.Utf8ResponseName;

        WriteByte((byte)'"', ref segments, ref currentChunkIndex, ref currentChunkOffset);
        WriteBytes(nameBytes, ref segments, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'"', ref segments, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteString(
        int index,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var data = _data.WrittenSpan.Slice(row.Location, row.SizeOrLength);
        WriteBytes(data, ref segments, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteNumber(
        int index,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var data = _data.WrittenSpan.Slice(row.Location, row.SizeOrLength);
        WriteBytes(data, ref segments, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteByte(
        byte value,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var seg = segments[currentChunkIndex];

        if (currentChunkOffset >= seg.Length)
        {
            seg = MoveToNextChunk(ref segments, ref currentChunkIndex, ref currentChunkOffset);
        }

        var buf = seg.Buffer;
        var baseOff = seg.Offset;
        buf[baseOff + currentChunkOffset] = value;
        currentChunkOffset++;
    }

    private void WriteBytes(
        ReadOnlySpan<byte> data,
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var remaining = data.Length;
        var dataOffset = 0;

        while (remaining > 0)
        {
            var seg = segments[currentChunkIndex];

            if (currentChunkOffset >= seg.Length)
            {
                seg = MoveToNextChunk(ref segments, ref currentChunkIndex, ref currentChunkOffset);
            }

            var available = seg.Length - currentChunkOffset;
            var bytesToWrite = Math.Min(remaining, available);

            data.Slice(dataOffset, bytesToWrite)
                .CopyTo(seg.Buffer.AsSpan(seg.Offset + currentChunkOffset, bytesToWrite));

            currentChunkOffset += bytesToWrite;
            dataOffset += bytesToWrite;
            remaining -= bytesToWrite;
        }
    }

    private MemorySegment MoveToNextChunk(
        ref MemorySegment[] segments,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var nextChunkIndex = currentChunkIndex + 1;

        if (nextChunkIndex >= SourceResultDocument.DataMaxChunks)
        {
            throw new InvalidOperationException(
                "The source result document has exceeded its maximum data capacity.");
        }

        if (nextChunkIndex >= segments.Length)
        {
            _arena.GrowSegmentTable(ref segments);
        }

        var segment = _arena.Rent(SourceResultDocument.GetDataChunkSize(nextChunkIndex));
        segments[nextChunkIndex] = segment;
        currentChunkIndex = nextChunkIndex;
        currentChunkOffset = 0;
        return segment;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _data.Dispose();
            _metaDb.Dispose();
            _disposed = true;
        }
    }
}

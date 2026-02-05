using System.Buffers;
using System.Diagnostics;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Text.Json;

internal sealed partial class SourceResultDocumentBuilder : IDisposable
{
    private readonly Operation _operation;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal readonly PooledArrayWriter _data = new();

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    internal MetaDb _metaDb;

    private bool _disposed;

    public SourceResultDocumentBuilder(Operation operation, ulong includeFlags, SelectionSet? selectionSet = null)
    {
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

        // Rent a jagged array to hold up to 64 chunks (64 * 128KB = 8MB max)
        var chunks = ArrayPool<byte[]>.Shared.Rent(64);

        // Initialize all slots to empty arrays
        for (var i = 0; i < chunks.Length; i++)
        {
            chunks[i] = [];
        }

        // Rent the first chunk
        chunks[0] = JsonMemory.Rent();

        var currentChunkIndex = 0;
        var currentChunkOffset = 0;

        try
        {
            WriteElement(0, chunks, ref currentChunkIndex, ref currentChunkOffset);

            var usedChunks = currentChunkIndex + 1;
            var lastChunkLength = currentChunkOffset;

            return SourceResultDocument.Parse(chunks, lastChunkLength, usedChunks, pooledMemory: true);
        }
        catch
        {
            JsonMemory.Return(chunks, currentChunkIndex + 1);
            ArrayPool<byte[]>.Shared.Return(chunks);
            throw;
        }
    }

    private void WriteElement(
        int index,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        switch (_metaDb.GetElementTokenType(index))
        {
            case ElementTokenType.StartObject:
                WriteObject(index, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.StartArray:
                WriteArray(index, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.String:
                WriteString(index, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.Number:
                WriteNumber(index, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.True:
                WriteBytes("true"u8, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.False:
                WriteBytes("false"u8, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.None:
            case ElementTokenType.Null:
                WriteBytes("null"u8, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;

            case ElementTokenType.Reference:
                var referenceIndex = _metaDb.GetLocation(index);
                WriteElement(referenceIndex, chunks, ref currentChunkIndex, ref currentChunkOffset);
                break;
        }
    }

    private void WriteObject(
        int startIndex,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        WriteByte((byte)'{', chunks, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'\n', chunks, ref currentChunkIndex, ref currentChunkOffset);

        startIndex = GetStartIndex(startIndex);
        var current = startIndex;
        var endIndex = GetEndIndex(startIndex) - 1;

        while (current < endIndex)
        {
            if (current > startIndex)
            {
                WriteByte((byte)',', chunks, ref currentChunkIndex, ref currentChunkOffset);
            }

            WritePropertyName(++current, chunks, ref currentChunkIndex, ref currentChunkOffset);
            WriteByte((byte)':', chunks, ref currentChunkIndex, ref currentChunkOffset);
            WriteElement(++current, chunks, ref currentChunkIndex, ref currentChunkOffset);
            WriteByte((byte)'\n', chunks, ref currentChunkIndex, ref currentChunkOffset);
        }

        WriteByte((byte)'}', chunks, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'\n', chunks, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteArray(
        int startIndex,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        WriteByte((byte)'[', chunks, ref currentChunkIndex, ref currentChunkOffset);

        var row = _metaDb.Get(startIndex);
        var elementCount = row.SizeOrLength;
        var elementIndex = startIndex + 1;

        for (var i = 0; i < elementCount; i++)
        {
            if (i > 0)
            {
                WriteByte((byte)',', chunks, ref currentChunkIndex, ref currentChunkOffset);
            }

            WriteElement(elementIndex + i, chunks, ref currentChunkIndex, ref currentChunkOffset);
        }

        WriteByte((byte)']', chunks, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WritePropertyName(
        int index,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var selectionId = row.Location;

        // Get the selection to retrieve the UTF-8 response name
        var selection = _operation.GetSelectionById(selectionId);
        var nameBytes = selection.Utf8ResponseName;

        WriteByte((byte)'"', chunks, ref currentChunkIndex, ref currentChunkOffset);
        WriteBytes(nameBytes, chunks, ref currentChunkIndex, ref currentChunkOffset);
        WriteByte((byte)'"', chunks, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteString(
        int index,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var location = row.Location;
        var size = row.SizeOrLength;

        var data = _data.WrittenSpan.Slice(location, size);
        WriteBytes(data, chunks, ref currentChunkIndex, ref currentChunkOffset);
    }

    private void WriteNumber(
        int index,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var row = _metaDb.Get(index);
        var location = row.Location;
        var size = row.SizeOrLength;

        var data = _data.WrittenSpan.Slice(location, size);
        WriteBytes(data, chunks, ref currentChunkIndex, ref currentChunkOffset);
    }

    private static void WriteByte(
        byte value,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        EnsureChunkCapacity(chunks, ref currentChunkIndex, ref currentChunkOffset);
        chunks[currentChunkIndex][currentChunkOffset] = value;
        currentChunkOffset++;
    }

    private static void WriteBytes(
        ReadOnlySpan<byte> data,
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        var remaining = data.Length;
        var dataOffset = 0;

        while (remaining > 0)
        {
            EnsureChunkCapacity(chunks, ref currentChunkIndex, ref currentChunkOffset);

            var available = JsonMemory.BufferSize - currentChunkOffset;
            var bytesToWrite = Math.Min(remaining, available);

            data.Slice(dataOffset, bytesToWrite).CopyTo(chunks[currentChunkIndex].AsSpan(currentChunkOffset));

            currentChunkOffset += bytesToWrite;
            dataOffset += bytesToWrite;
            remaining -= bytesToWrite;
        }
    }

    private static void EnsureChunkCapacity(
        byte[][] chunks,
        ref int currentChunkIndex,
        ref int currentChunkOffset)
    {
        if (currentChunkOffset >= JsonMemory.BufferSize)
        {
            currentChunkIndex++;
            currentChunkOffset = 0;
        }

        // Rent a new chunk if needed (and it's not already rented)
        if (chunks[currentChunkIndex].Length == 0)
        {
            Debug.Fail("foo");

            chunks[currentChunkIndex] = JsonMemory.Rent();
        }
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

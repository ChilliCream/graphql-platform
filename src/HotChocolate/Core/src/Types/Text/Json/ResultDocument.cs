using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private readonly Path _rootPath = Path.Root;
    internal MetaDb _metaDb;
    private int _nextDataIndex;
    private int _rentedDataSize;
    private readonly List<byte[]> _data = [];
#if NET10_0_OR_GREATER
    private readonly Lock _dataChunkLock = new();
#else
    private readonly object _dataChunkLock = new();
#endif
    private bool _disposed;

    public ResultDocument(Operation operation, ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(operation);

        _metaDb = MetaDb.CreateForEstimatedRows(Cursor.RowsPerChunk);
        _operation = operation;
        _includeFlags = includeFlags;

        Data = CreateObject(Cursor.Zero, operation.RootSelectionSet);
    }

    public ResultDocument(
        Operation operation,
        SelectionSet selectionSet,
        Path path,
        ulong includeFlags,
        ulong deferFlags,
        DeferUsage deferUsage)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(deferUsage);

        _metaDb = MetaDb.CreateForEstimatedRows(Cursor.RowsPerChunk);
        _operation = operation;
        _includeFlags = includeFlags;
        _rootPath = path;

        Data = CreateObject(Cursor.Zero, selectionSet, includeFlags, deferFlags, deferUsage);
    }

    public ResultElement Data { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ElementTokenType GetElementTokenType(Cursor cursor)
        => _metaDb.GetElementTokenType(cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Operation GetOperation()
        => _operation;

    internal SelectionSet? GetSelectionSet(Cursor cursor)
    {
        var row = _metaDb.Get(cursor);

        return row.OperationReferenceType is OperationReferenceType.SelectionSet
            ? _operation.GetSelectionSetById(row.OperationReferenceId)
            : null;
    }

    internal Selection? GetSelection(Cursor cursor)
    {
        if (cursor == Cursor.Zero)
        {
            return null;
        }

        // If the cursor points at a value, step back to the PropertyName row.
        var row = _metaDb.Get(cursor);

        if (row.TokenType is not ElementTokenType.PropertyName)
        {
            cursor = cursor.AddRows(-1);
            row = _metaDb.Get(cursor);

            if (row.TokenType is not ElementTokenType.PropertyName)
            {
                return null;
            }
        }

        return row.OperationReferenceType is OperationReferenceType.Selection
            ? _operation.GetSelectionById(row.OperationReferenceId)
            : null;
    }

    internal ResultElement GetArrayIndexElement(Cursor current, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var (start, tokenType) = _metaDb.GetStartCursor(current);

        CheckExpectedType(ElementTokenType.StartArray, tokenType);

        var len = _metaDb.GetNumberOfRows(start);

        if ((uint)arrayIndex >= (uint)len)
        {
            throw new IndexOutOfRangeException();
        }

        // first element is at +1 after StartArray
        return new ResultElement(this, start.AddRows(arrayIndex + 1));
    }

    internal int GetArrayLength(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (current, var tokenType) = _metaDb.GetStartCursor(current);

        CheckExpectedType(ElementTokenType.StartArray, tokenType);

        return _metaDb.GetSizeOrLength(current);
    }

    internal int GetPropertyCount(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        (current, var tokenType) = _metaDb.GetStartCursor(current);

        CheckExpectedType(ElementTokenType.StartObject, tokenType);

        return _metaDb.GetSizeOrLength(current);
    }

    internal Path CreatePath(Cursor current)
    {
        // Stop at root via IsRoot flag.
        if ((_metaDb.GetFlags(current) & ElementFlags.IsRoot) == ElementFlags.IsRoot)
        {
            return _rootPath;
        }

        Span<Cursor> chain = stackalloc Cursor[64];
        var c = current;
        var written = 0;

        while (true)
        {
            chain[written++] = c;

            var parentIndex = _metaDb.GetParent(c);
            if (parentIndex <= 0)
            {
                break;
            }

            c = Cursor.FromIndex(parentIndex);

            if (written >= 64)
            {
                throw new InvalidOperationException("The path is to deep.");
            }
        }

        var path = _rootPath;
        var parentTokenType = ElementTokenType.StartObject;

        chain = chain[..written];

        for (var i = chain.Length - 1; i >= 0; i--)
        {
            c = chain[i];
            var tokenType = _metaDb.GetElementTokenType(c, resolveReferences: false);

            if (tokenType == ElementTokenType.PropertyName)
            {
                path = path.Append(GetSelection(c)!.ResponseName);
                i--; // skip over the actual value
            }
            else if (chain.Length - 1 > i)
            {
                var parentCursor = chain[i + 1];

                if (parentTokenType is ElementTokenType.StartArray)
                {
                    // arrayIndex = abs(child) - (abs(parent) + 1)
                    var absChild = (c.Chunk * Cursor.RowsPerChunk) + c.Row;
                    var absParent = (parentCursor.Chunk * Cursor.RowsPerChunk) + parentCursor.Row;
                    var arrayIndex = absChild - (absParent + 1);
                    path = path.Append(arrayIndex);
                }
            }

            parentTokenType = tokenType;
        }

        return path;
    }

    internal ResultElement GetParent(Cursor current)
    {
        // The null cursor represents the data object, which is the utmost root.
        // If we have reached that we simply return an undefined element
        if (current == Cursor.Zero)
        {
            return default;
        }

        var parent = _metaDb.GetParentCursor(current);

        // if the parent element is a property name then we must get the parent of that,
        // as property name and value represent the same element.
        if (_metaDb.GetElementTokenType(parent) is ElementTokenType.PropertyName)
        {
            parent = _metaDb.GetParentCursor(parent);
        }

        // if we have not yet reached the root and the element type of the parent is an object or an array
        // then we need to get still the parent of this row as we want to get the logical parent
        // which is the value level of the property or the element in an array.
        if (parent != Cursor.Zero
            && _metaDb.GetElementTokenType(parent) is ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            parent = _metaDb.GetParentCursor(parent);

            // in this case the parent must be a reference, otherwise we would have
            // found an inconsistency in the database.
            Debug.Assert(_metaDb.GetElementTokenType(parent, resolveReferences: false) == ElementTokenType.Reference);
        }

        return new ResultElement(this, parent);
    }

    internal bool IsInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tokenType = _metaDb.GetElementTokenType(current, resolveReferences: false);

        if (tokenType is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(current);
            return (flags & ElementFlags.IsInvalidated) == ElementFlags.IsInvalidated;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                return (flags & ElementFlags.IsInvalidated) == ElementFlags.IsInvalidated;
            }
        }

        return false;
    }

    internal bool IsNullOrInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tokenType = _metaDb.GetElementTokenType(current);

        if (tokenType is ElementTokenType.Null or ElementTokenType.None)
        {
            return true;
        }

        if (tokenType is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(current);
            return (flags & ElementFlags.IsInvalidated) == ElementFlags.IsInvalidated;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                return (flags & ElementFlags.IsInvalidated) == ElementFlags.IsInvalidated;
            }
        }

        return false;
    }

    internal bool IsInternalProperty(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // The flag sits on the property row (one before value)
        var propertyCursor = current.AddRows(-1);
        var flags = _metaDb.GetFlags(propertyCursor);
        return (flags & ElementFlags.IsInternal) == ElementFlags.IsInternal;
    }

    internal void Invalidate(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tokenType = _metaDb.GetElementTokenType(current, resolveReferences: false);

        if (tokenType is ElementTokenType.None)
        {
            return;
        }

        if (tokenType is ElementTokenType.StartArray)
        {
            return;
        }

        if (tokenType is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(current);
            _metaDb.SetFlags(current, flags | ElementFlags.IsInvalidated);
            return;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                _metaDb.SetFlags(current, flags | ElementFlags.IsInvalidated);
            }

            return;
        }

        Debug.Fail("Only objects can be invalidated.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteRawValueTo(Utf8JsonWriter writer, DbRow row)
    {
        switch (row.TokenType)
        {
            case ElementTokenType.Null:
                writer.WriteNullValue();
                return;

            case ElementTokenType.True:
                writer.WriteBooleanValue(true);
                return;

            case ElementTokenType.False:
                writer.WriteBooleanValue(false);
                return;

            case ElementTokenType.String:
            case ElementTokenType.Number:
                writer.WriteRawValue(ReadRawValue(row), skipInputValidation: true);
                return;

            default:
                throw new NotSupportedException();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteToBuffer(IBufferWriter<byte> writer, ReadOnlySpan<byte> data)
    {
        var span = writer.GetSpan(data.Length);
        data.CopyTo(span);
        writer.Advance(data.Length);
    }

    /// <summary>
    /// Writes local data to the buffer, handling chunk boundaries.
    /// </summary>
    private void WriteLocalDataTo(IBufferWriter<byte> writer, int location, int size)
    {
        var remaining = size;
        var currentPos = location;

        while (remaining > 0)
        {
            var chunkIndex = currentPos / JsonMemory.BufferSize;
            var offset = currentPos % JsonMemory.BufferSize;
            var availableInChunk = JsonMemory.BufferSize - offset;
            var toWrite = Math.Min(remaining, availableInChunk);

            var source = _data[chunkIndex].AsSpan(offset, toWrite);
            var dest = writer.GetSpan(toWrite);
            source.CopyTo(dest);
            writer.Advance(toWrite);

            currentPos += toWrite;
            remaining -= toWrite;
        }
    }

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
    {
        switch (row.TokenType)
        {
            case ElementTokenType.Null:
                return JsonConstants.NullValue;

            case ElementTokenType.True:
                return JsonConstants.TrueValue;

            case ElementTokenType.False:
                return JsonConstants.FalseValue;

            case ElementTokenType.PropertyName when row.OperationReferenceType is OperationReferenceType.Selection:
                return _operation.GetSelectionById(row.OperationReferenceId).Utf8ResponseName;

            case ElementTokenType.PropertyName:
            case ElementTokenType.String:
            case ElementTokenType.Number:
                return ReadLocalData(row.Location, row.SizeOrLength);

            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Reads local data from the data chunks.
    /// </summary>
    /// <remarks>
    /// This method only supports data that fits within a single chunk.
    /// Data that spans chunk boundaries should use <see cref="WriteLocalDataTo"/> instead.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadLocalData(int location, int size)
    {
        var startChunkIndex = location / JsonMemory.BufferSize;
        var offsetInStartChunk = location % JsonMemory.BufferSize;

        // Fast path: data fits in a single chunk
        if (offsetInStartChunk + size <= JsonMemory.BufferSize)
        {
            return _data[startChunkIndex].AsSpan(offsetInStartChunk, size);
        }

        Span<byte> buffer = new byte[size];
        var bytesRead = 0;
        var currentLocation = location;

        while (bytesRead < size)
        {
            var chunkIndex = currentLocation / JsonMemory.BufferSize;
            var offsetInChunk = currentLocation % JsonMemory.BufferSize;
            var chunk = _data[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, JsonMemory.BufferSize - offsetInChunk);
            var chunkSpan = chunk.AsSpan(offsetInChunk, bytesToCopyFromThisChunk);

            chunkSpan.CopyTo(buffer[bytesRead..]);
            bytesRead += bytesToCopyFromThisChunk;
            currentLocation += bytesToCopyFromThisChunk;
        }

        return buffer;
    }

    internal ResultElement CreateObject(Cursor parent, SelectionSet selectionSet)
    {
        lock (_dataChunkLock)
        {
            var startObjectCursor = WriteStartObject(parent, selectionSet.Id);

            var selectionCount = 0;
            foreach (var selection in selectionSet.Selections)
            {
                WriteEmptyProperty(startObjectCursor, selection);
                selectionCount++;
            }

            WriteEndObject(startObjectCursor, selectionCount);

            return new ResultElement(this, startObjectCursor);
        }
    }

    private ResultElement CreateObject(
        Cursor parent,
        SelectionSet selectionSet,
        ulong includeFlags,
        ulong deferFlags,
        DeferUsage deferUsage)
    {
        lock (_dataChunkLock)
        {
            var startObjectCursor = WriteStartObject(parent, selectionSet.Id);

            var selectionCount = 0;
            foreach (var selection in selectionSet.Selections)
            {
                if (selection.IsIncluded(includeFlags)
                    && selection.IsDeferred(deferFlags)
                    && selection.GetPrimaryDeferUsage(deferFlags) == deferUsage)
                {
                    WriteEmptyProperty(startObjectCursor, selection);
                    selectionCount++;
                }
            }

            WriteEndObject(startObjectCursor, selectionCount);

            return new ResultElement(this, startObjectCursor);
        }
    }

    internal ResultElement CreateObject(Cursor parent, int propertyCount)
    {
        lock (_dataChunkLock)
        {
            var startObjectCursor = WriteStartObject(parent, isSelectionSet: false);

            for (var i = 0; i < propertyCount; i++)
            {
                WriteEmptyProperty(startObjectCursor);
            }

            WriteEndObject(startObjectCursor, propertyCount);

            return new ResultElement(this, startObjectCursor);
        }
    }

    internal ResultElement CreateArray(Cursor parent, int length)
    {
        lock (_dataChunkLock)
        {
            var cursor = WriteStartArray(parent, length);

            for (var i = 0; i < length; i++)
            {
                WriteEmptyValue(cursor);
            }

            WriteEndArray();

            return new ResultElement(this, cursor);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignObjectOrArray(ResultElement target, ResultElement value)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Reference,
            location: value.Cursor.ToIndex(),
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignStringValue(ResultElement target, ReadOnlySpan<byte> value, bool isEncoded)
    {
        var totalSize = value.Length + 2;
        var position = ClaimDataSpace(totalSize);
        WriteData(position, value, withQuotes: true);

        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.String,
            location: position,
            sizeOrLength: totalSize,
            parentRow: _metaDb.GetParent(target.Cursor),
            flags: isEncoded ? ElementFlags.IsEncoded : ElementFlags.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignPropertyName(ResultElement target, ReadOnlySpan<byte> propertyName)
    {
        var cursor = target.Cursor - 1;
        var row = _metaDb.Get(cursor);
        Debug.Assert(row.TokenType is ElementTokenType.PropertyName);
        Debug.Assert(row.OperationReferenceType is OperationReferenceType.None);

        var totalSize = propertyName.Length;
        var position = ClaimDataSpace(totalSize);
        WriteData(position, propertyName, withQuotes: false);

        _metaDb.Replace(
            cursor: cursor,
            tokenType: ElementTokenType.PropertyName,
            location: position,
            sizeOrLength: totalSize,
            parentRow: row.ParentRow);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNumberValue(ResultElement target, ReadOnlySpan<byte> value)
    {
        var position = ClaimDataSpace(value.Length);
        WriteData(position, value);

        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Number,
            location: position,
            sizeOrLength: value.Length,
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignBooleanValue(ResultElement target, bool value)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: value ? ElementTokenType.True : ElementTokenType.False,
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNullValue(ResultElement target)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Null,
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void MarkAsDeferred(ResultElement target)
    {
        // Selection metadata and write filters are tracked on the property row.
        var propertyCursor = target.Cursor.AddRows(-1);
        var elementTokenType = _metaDb.GetElementTokenType(propertyCursor, resolveReferences: false);

        CheckExpectedType(ElementTokenType.PropertyName, elementTokenType);

        var flags = _metaDb.GetFlags(propertyCursor);
        _metaDb.SetFlags(propertyCursor, flags | ElementFlags.IsDeferred);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ClaimDataSpace(int size)
    {
        // Atomically claim space
        var endPosition = Interlocked.Add(ref _nextDataIndex, size);
        var startPosition = endPosition - size;

        // Fast path: we check if we already have enough rented memory
        // if so we can directly return and write the data without locking.
        if (endPosition <= Volatile.Read(ref _rentedDataSize))
        {
            return startPosition;
        }

        // Slow path: we need to rent more chunks so in this case
        // we will need to do a proper synchronization.
        EnsureDataCapacity(endPosition);
        return startPosition;
    }

    private void EnsureDataCapacity(int requiredSize)
    {
        lock (_dataChunkLock)
        {
            // Double-check after acquiring lock
            var currentSize = _rentedDataSize;
            if (requiredSize <= currentSize)
            {
                return;
            }

            // Rent chunks until we have enough
            while (currentSize < requiredSize)
            {
                _data.Add(JsonMemory.Rent(JsonMemoryKind.Json));
                currentSize += JsonMemory.BufferSize;
            }

            // Publish new size (volatile write)
            Volatile.Write(ref _rentedDataSize, currentSize);
        }
    }

    /// <summary>
    /// Writes data to the data chunks, handling chunk boundaries.
    /// </summary>
    /// <param name="position">The position to start writing at.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="withQuotes">If true, wraps the data with JSON quotes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteData(int position, ReadOnlySpan<byte> data, bool withQuotes = false)
    {
        if (withQuotes)
        {
            WriteByte(position, JsonConstants.Quote);
            WriteDataCore(position + 1, data);
            WriteByte(position + 1 + data.Length, JsonConstants.Quote);
        }
        else
        {
            WriteDataCore(position, data);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteByte(int position, byte value)
    {
        var chunkIndex = position / JsonMemory.BufferSize;
        var offset = position % JsonMemory.BufferSize;
        _data[chunkIndex][offset] = value;
    }

    private void WriteDataCore(int position, ReadOnlySpan<byte> data)
    {
        var chunkIndex = position / JsonMemory.BufferSize;
        var offset = position % JsonMemory.BufferSize;
        var availableInChunk = JsonMemory.BufferSize - offset;

        // Fast path: we can write all the data into single chunk
        if (data.Length <= availableInChunk)
        {
            data.CopyTo(_data[chunkIndex].AsSpan(offset, data.Length));
            return;
        }

        // Slow path: data spans multiple chunks so we need to loop
        var remaining = data;
        var currentPos = position;

        while (remaining.Length > 0)
        {
            chunkIndex = currentPos / JsonMemory.BufferSize;
            offset = currentPos % JsonMemory.BufferSize;
            availableInChunk = JsonMemory.BufferSize - offset;
            var toWrite = Math.Min(remaining.Length, availableInChunk);

            remaining[..toWrite].CopyTo(_data[chunkIndex].AsSpan(offset, toWrite));

            currentPos += toWrite;
            remaining = remaining[toWrite..];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartObject(Cursor parent, int selectionSetId = 0, bool isSelectionSet = true)
    {
        var flags = ElementFlags.None;
        var parentRow = ToIndex(parent);

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.Append(
            ElementTokenType.StartObject,
            parentRow: parentRow,
            operationReferenceId: selectionSetId,
            operationReferenceType: isSelectionSet
                ? OperationReferenceType.SelectionSet
                : OperationReferenceType.None,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndObject(Cursor startObjectCursor, int length)
    {
        _metaDb.Append(ElementTokenType.EndObject);

        _metaDb.SetNumberOfRows(startObjectCursor, (length * 2) + 1);
        _metaDb.SetSizeOrLength(startObjectCursor, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartArray(Cursor parent, int length = 0)
    {
        var flags = ElementFlags.None;
        var parentRow = ToIndex(parent);

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.Append(
            ElementTokenType.StartArray,
            sizeOrLength: length,
            parentRow: parentRow,
            numberOfRows: length + 1,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndArray() => _metaDb.Append(ElementTokenType.EndArray);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(Cursor parent, ISelection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (!selection.IsIncluded(_includeFlags))
        {
            flags |= ElementFlags.IsExcluded;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        if (selection.Type.IsListType())
        {
            flags |= ElementFlags.IsList;
        }

        if (selection.Type.NamedType().IsCompositeType())
        {
            flags |= ElementFlags.IsObject;
        }

        var prop = _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: ToIndex(parent),
            operationReferenceId: selection.Id,
            operationReferenceType: OperationReferenceType.Selection,
            flags: flags);

        _metaDb.Append(
            ElementTokenType.None,
            parentRow: ToIndex(prop));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(Cursor parent)
    {
        var prop = _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: ToIndex(parent));

        _metaDb.Append(
            ElementTokenType.None,
            parentRow: ToIndex(prop));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyValue(Cursor parent)
    {
        _metaDb.Append(
            ElementTokenType.None,
            parentRow: ToIndex(parent));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ToIndex(Cursor c) => (c.Chunk * Cursor.RowsPerChunk) + c.Row;

    private static void CheckExpectedType(ElementTokenType expected, ElementTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but found {actual}.");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _metaDb.Dispose();

            if (_data.Count > 0)
            {
                JsonMemory.Return(JsonMemoryKind.Json, _data);
            }

            _disposed = true;
        }
    }
}

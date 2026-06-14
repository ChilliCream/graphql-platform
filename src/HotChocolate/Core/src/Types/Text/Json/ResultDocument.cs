using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Text.Json;

public sealed partial class ResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private static readonly ArrayPool<MemorySegment> s_arrayPool = ArrayPool<MemorySegment>.Shared;
    private readonly IMemoryArena _arena;
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private readonly Path _rootPath = Path.Root;
    internal MetaDb _metaDb;
    private long _dataHead;
    private int _rentedDataChunks;
    private MemorySegment[] _data;

#if NET10_0_OR_GREATER
    private readonly Lock _dataChunkLock = new();
#else
    private readonly object _dataChunkLock = new();
#endif
    private int _disposed;

    public ResultDocument(
        IMemoryArena arena,
        Operation operation,
        ulong includeFlags)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(operation);

        _arena = arena;
        _metaDb = MetaDb.Create(arena);
        _data = RentDataChunks();
        _operation = operation;
        _includeFlags = includeFlags;

        Data = CreateObject(Cursor.CreateZero(), operation.RootSelectionSet);
    }

    internal ResultDocument(
        IMemoryArena arena,
        Operation operation,
        SelectionSet selectionSet,
        Path path,
        ulong includeFlags,
        ulong deferFlags,
        DeferUsage deferUsage)
    {
        ArgumentNullException.ThrowIfNull(arena);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(selectionSet);
        ArgumentNullException.ThrowIfNull(deferUsage);

        _arena = arena;
        _metaDb = MetaDb.Create(arena);
        _data = RentDataChunks();
        _operation = operation;
        _includeFlags = includeFlags;
        _rootPath = path;

        Data = CreateObject(Cursor.CreateZero(), selectionSet, includeFlags, deferFlags, deferUsage);
    }

    private static MemorySegment[] RentDataChunks()
    {
        var data = s_arrayPool.Rent(4);

        // Clear pooled slots so unallocated chunks are recognizable by a null buffer.
        data.AsSpan().Clear();
        return data;
    }

    public ResultElement Data { get; }

    // A data location packs the chunk index in the high 12 bits and the byte offset within the
    // chunk in the low 17 bits, fitting the 29-bit location field of a metadb row. The chunk size
    // follows the same geometric schedule as the metadb, so it is derived from the chunk index.
    private const int DataOffsetBits = 17;
    private const int DataChunkBits = 12;
    private const int DataOffsetMask = (1 << DataOffsetBits) - 1;
    private const int DataMaxChunks = 1 << DataChunkBits;
    private const int DataMaxChunkOrdinal = (int)ChunkSize.Size128K;

    // The geometric ramp covers chunks 0..7 (Size1K..Size128K); chunk 8 and beyond stay at
    // Size128K. The ramp holds 1024 * (2^0 + ... + 2^7) = 255 KB before the constant tail begins.
    private const int RampChunkCount = DataMaxChunkOrdinal + 1;
    private const int LargestChunkBytes = 1 << (10 + DataMaxChunkOrdinal);
    private const long RampTotalBytes = (long)255 * 1024;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDataChunkSize(int chunkIndex)
        => 1 << (10 + Math.Min(chunkIndex, DataMaxChunkOrdinal));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int EncodeDataLocation(int chunkIndex, int offset)
        => (chunkIndex << DataOffsetBits) | offset;

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
        if (cursor.IsZero)
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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        (current, var tokenType) = _metaDb.GetStartCursor(current);

        CheckExpectedType(ElementTokenType.StartArray, tokenType);

        return _metaDb.GetSizeOrLength(current);
    }

    internal int GetPropertyCount(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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

            var parent = _metaDb.GetParentCursor(c);
            if (parent.IsZero)
            {
                break;
            }

            c = parent;

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
                    var arrayIndex = c.Index - (parentCursor.Index + 1);
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
        if (current.IsZero)
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
        if (!parent.IsZero
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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        // The flag sits on the property row (one before value)
        var propertyCursor = current.AddRows(-1);
        var flags = _metaDb.GetFlags(propertyCursor);
        return (flags & ElementFlags.IsInternal) == ElementFlags.IsInternal;
    }

    internal void Invalidate(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

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
    /// Reads local data from the data chunks. Data contained in a single chunk is returned as a
    /// slice over that chunk; data that spans chunk boundaries is copied into a fresh buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> ReadLocalData(int location, int size)
    {
        if (size == 0)
        {
            return [];
        }

        var startChunkIndex = location >>> DataOffsetBits;
        var offsetInStartChunk = location & DataOffsetMask;

        // Fast path: data fits in a single chunk
        if (offsetInStartChunk + size <= GetDataChunkSize(startChunkIndex))
        {
            var seg = _data[startChunkIndex];
            return seg.Buffer.AsSpan(seg.Offset + offsetInStartChunk, size);
        }

        Span<byte> buffer = new byte[size];
        var bytesRead = 0;
        var chunkIndex = startChunkIndex;
        var offsetInChunk = offsetInStartChunk;

        while (bytesRead < size)
        {
            var chunk = _data[chunkIndex];

            var bytesToCopyFromThisChunk = Math.Min(size - bytesRead, GetDataChunkSize(chunkIndex) - offsetInChunk);
            var chunkSpan = chunk.Buffer.AsSpan(chunk.Offset + offsetInChunk, bytesToCopyFromThisChunk);

            chunkSpan.CopyTo(buffer[bytesRead..]);
            bytesRead += bytesToCopyFromThisChunk;
            chunkIndex++;
            offsetInChunk = 0;
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
                    && selection.HasActiveDeferUsage(deferFlags, deferUsage))
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
            location: value.Cursor.Value,
            parent: _metaDb.GetParent(target.Cursor));
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
            parent: _metaDb.GetParent(target.Cursor),
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
            parent: row.Parent);
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
            parent: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignBooleanValue(ResultElement target, bool value)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: value ? ElementTokenType.True : ElementTokenType.False,
            parent: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNullValue(ResultElement target)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Null,
            parent: _metaDb.GetParent(target.Cursor));
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
        // The data store is a single linear byte space, so a claim is one atomic add on the head.
        // The start is the head before the add; the schedule decode below maps it to (chunk, offset).
        var end = Interlocked.Add(ref _dataHead, size);
        var (location, lastChunk) = ComputeDataClaim(end - size, size);
        EnsureDataChunks(lastChunk);
        return location;
    }

    /// <summary>
    /// Computes the claim for <paramref name="size"/> bytes starting at the linear byte
    /// position <paramref name="start"/>: the encoded start location and the chunk holding the
    /// last byte.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The claim would exceed the document's data capacity.
    /// </exception>
    internal static (int Location, int LastChunk) ComputeDataClaim(long start, int size)
    {
        // A value always starts exactly at the head so the data store is written without gaps;
        // a value that does not fit the remaining space of the current chunk spans into the
        // following chunks, and the read and write paths walk the boundaries.
        var (chunk, offset) = DecodeDataLocation(start);

        // The last byte of the value (size == 0 claims occupy no byte, so they stay in chunk).
        var lastChunk = size > 0 ? DecodeDataLocation(start + size - 1).Chunk : chunk;

        if (lastChunk >= DataMaxChunks)
        {
            throw new InvalidOperationException(
                "The result document has exceeded its maximum data capacity.");
        }

        return (EncodeDataLocation(chunk, offset), lastChunk);
    }

    /// <summary>
    /// Maps a linear byte position to a (chunk, offset) location following the geometric schedule.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (int Chunk, int Offset) DecodeDataLocation(long pos)
    {
        // Ramp (chunks 0..7): each chunk c holds 1024 << c bytes, so the cumulative bytes before
        // chunk c are 1024 * ((1 << c) - 1). The chunk index is the highest bit set in
        // (pos >> 10) + 1, which Log2 returns branch-free.
        if (pos < RampTotalBytes)
        {
            var chunk = BitOperations.Log2((uint)((pos >> 10) + 1));
            var offset = (int)(pos - ((1L << (10 + chunk)) - 1024));
            return (chunk, offset);
        }

        // Tail (chunk 8+): every chunk holds the largest size (128K), so the index is closed-form.
        var tail = pos - RampTotalBytes;
        return (RampChunkCount + (int)(tail >> 17), (int)(tail & (LargestChunkBytes - 1)));
    }

    private void EnsureDataChunks(int requiredChunkIndex)
    {
        // Fast path: the chunk has already been rented.
        if (requiredChunkIndex < Volatile.Read(ref _rentedDataChunks))
        {
            return;
        }

        lock (_dataChunkLock)
        {
            var data = _data;
            var rented = _rentedDataChunks;

            if (requiredChunkIndex >= data.Length)
            {
                // Double the tracking array, copy the rented segments, and clear the new slots so
                // unallocated chunks are recognizable by a null buffer. The arena owns the chunk
                // memory itself, so only the pooled tracking array is grown here.
                var nextLength = data.Length * 2;
                var newData = s_arrayPool.Rent(nextLength);
                Array.Copy(data, newData, rented);
                newData.AsSpan(rented).Clear();
                s_arrayPool.Return(data);
                _data = data = newData;
            }

            while (rented <= requiredChunkIndex)
            {
                data[rented] = _arena.Rent(GetDataChunkSize(rented));
                rented++;
            }

            Volatile.Write(ref _rentedDataChunks, rented);
        }
    }

    /// <summary>
    /// Writes data to the data chunks, handling chunk boundaries.
    /// </summary>
    /// <param name="location">The encoded (chunk, offset) location to start writing at.</param>
    /// <param name="data">The data to write.</param>
    /// <param name="withQuotes">If true, wraps the data with JSON quotes.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteData(int location, ReadOnlySpan<byte> data, bool withQuotes = false)
    {
        var chunk = location >>> DataOffsetBits;
        var offset = location & DataOffsetMask;

        if (withQuotes)
        {
            (chunk, offset) = WriteByte(chunk, offset, JsonConstants.Quote);
            (chunk, offset) = WriteDataCore(chunk, offset, data);
            WriteByte(chunk, offset, JsonConstants.Quote);
        }
        else
        {
            WriteDataCore(chunk, offset, data);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (int Chunk, int Offset) WriteByte(int chunk, int offset, byte value)
    {
        if (offset == GetDataChunkSize(chunk))
        {
            chunk++;
            offset = 0;
        }

        var segment = _data[chunk];
        segment.Buffer[segment.Offset + offset] = value;
        return (chunk, offset + 1);
    }

    private (int Chunk, int Offset) WriteDataCore(int chunk, int offset, ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
        {
            return (chunk, offset);
        }

        var availableInChunk = GetDataChunkSize(chunk) - offset;

        // Fast path: we can write all the data into a single chunk
        if (data.Length <= availableInChunk)
        {
            var seg = _data[chunk];
            data.CopyTo(seg.Buffer.AsSpan(seg.Offset + offset, data.Length));
            return (chunk, offset + data.Length);
        }

        // Slow path: data spans multiple chunks so we need to loop
        var remaining = data;

        while (remaining.Length > 0)
        {
            availableInChunk = GetDataChunkSize(chunk) - offset;
            var toWrite = Math.Min(remaining.Length, availableInChunk);

            var seg = _data[chunk];
            remaining[..toWrite].CopyTo(seg.Buffer.AsSpan(seg.Offset + offset, toWrite));

            remaining = remaining[toWrite..];
            offset += toWrite;

            if (offset == GetDataChunkSize(chunk))
            {
                chunk++;
                offset = 0;
            }
        }

        return (chunk, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartObject(Cursor parent, int selectionSetId = 0, bool isSelectionSet = true)
    {
        return _metaDb.Append(
            ElementTokenType.StartObject,
            parent: parent.Value,
            operationReferenceId: selectionSetId,
            operationReferenceType: isSelectionSet
                ? OperationReferenceType.SelectionSet
                : OperationReferenceType.None);
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
        return _metaDb.Append(
            ElementTokenType.StartArray,
            sizeOrLength: length,
            parent: parent.Value,
            numberOfRows: length + 1);
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
            parent: parent.Value,
            operationReferenceId: selection.Id,
            operationReferenceType: OperationReferenceType.Selection,
            flags: flags);

        _metaDb.Append(
            ElementTokenType.None,
            parent: prop.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(Cursor parent)
    {
        var prop = _metaDb.Append(
            ElementTokenType.PropertyName,
            parent: parent.Value);

        _metaDb.Append(
            ElementTokenType.None,
            parent: prop.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyValue(Cursor parent)
    {
        _metaDb.Append(
            ElementTokenType.None,
            parent: parent.Value);
    }

    private static void CheckExpectedType(ElementTokenType expected, ElementTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but found {actual}.");
        }
    }

    public void Dispose()
    {
        ReleaseTrackingArrays();
        GC.SuppressFinalize(this);
    }

    private void ReleaseTrackingArrays()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        // The arena owns the chunk memory and frees it as a whole when it is disposed, so the
        // data segments are simply dropped here. The metadb only releases its pooled tracking
        // arrays, not the chunk memory.
        _metaDb.Dispose();
        _data.AsSpan().Clear();
        s_arrayPool.Return(_data);
        _data = [];
    }

    ~ResultDocument() => ReleaseTrackingArrays();
}

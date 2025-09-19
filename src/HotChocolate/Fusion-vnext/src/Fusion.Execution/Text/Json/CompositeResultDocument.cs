using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;
using static HotChocolate.Fusion.Text.Json.MetaDbConstants;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private MetaDb _metaDb;
    private byte[][] _dataChunks;
    private List<SourceResultDocument> _sources = [];
    private Operation _operation;
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    private bool _disposed;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

    public CompositeResultDocument(Operation operation)
    {
        _metaDb = MetaDb.CreateForEstimatedRows(RowsPerChunk * 8);

        // we initialize the data chunks so that we can store local data on this document.
        _dataChunks = new byte[16][];
        _dataChunks[0] = JsonMemoryPool.Rent();
        _operation =  operation;

        // we create the root data object.
        Data = CreateObject(0, operation.RootSelectionSet);
    }

    public CompositeResultElement Data { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ElementTokenType GetElementTokenType(int index)
        => _metaDb.GetElementTokenType(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Operation GetOperation()
        => _operation;

    internal SelectionSet? GetSelectionSet(int index)
    {
        var row = _metaDb.Get(index);

        if (row.OperationReferenceType is not OperationReferenceType.SelectionSet)
        {
            return null;
        }

        return _operation.GetSelectionSetById(row.OperationReferenceId);
    }

    internal Selection? GetSelection(int index)
    {
        var row = _metaDb.Get(index);

        if (row.OperationReferenceType is not OperationReferenceType.Selection)
        {
            return null;
        }

        return _operation.GetSelectionById(row.OperationReferenceId);
    }

    internal CompositeResultElement GetArrayIndexElement(int currentIndex, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        var arrayLength = row.NumberOfRows;

        if ((uint)arrayIndex >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        return new CompositeResultElement(this, currentIndex + arrayIndex + 1);
    }

    internal int GetArrayLength(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        return row.NumberOfRows;
    }

    internal int GetPropertyCount(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(currentIndex);

        CheckExpectedType(ElementTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
    {
        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.OperationReferenceId).RawResponseName;
        }

        if (row.TokenType == ElementTokenType.Reference)
        {
        }

        throw new NotImplementedException();
    }

    private ReadOnlyMemory<byte> ReadRawValueAsMemory(DbRow row)
    {
        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.OperationReferenceId).RawResponseNameAsMemory;
        }

        if (row.TokenType == ElementTokenType.Reference)
        {
        }

        throw new NotImplementedException();
    }

    internal CompositeResultElement CreateObject(int parentRow, SelectionSet selectionSet)
    {
        var index = WriteStartObject(
            parentRow,
            selectionSet.Id,
            selectionSet.Selections.Length);

        foreach (var selection in selectionSet.Selections)
        {
            WriteEmptyProperty(index, selection);
        }

        WriteEndObject();

        return new CompositeResultElement(this, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignObjectValue(CompositeResultElement target, CompositeResultElement value)
    {
        _metaDb.Replace(
            index: target.Index,
            tokenType: ElementTokenType.Reference,
            location: value.Index,
            parentRow: _metaDb.GetParentRow(target.Index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignLeafValue(CompositeResultElement target, SourceResultElement source)
    {
        var value = source.GetValuePointer();

        _metaDb.Replace(
            index: target.Index,
            tokenType: source.TokenType.ToElementTokenType(),
            location: value.Location,
            sizeOrLength: value.Size,
            sourceDocumentId: source._parent.Id,
            parentRow: _metaDb.GetParentRow(target.Index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteStartObject(int parentRow = 0, int selectionSetId = 0, int length = 0)
    {
        var flags = ElementFlags.None;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.Append(
            ElementTokenType.StartObject,
            sizeOrLength: length,
            parentRow: parentRow,
            operationReferenceId: selectionSetId,
            operationReferenceType: OperationReferenceType.SelectionSet,
            numberOfRows: (length * 2) + 2,
            flags: flags );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndObject() => _metaDb.Append(ElementTokenType.EndObject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(int parentRow, Selection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (selection.IsLeaf)
        {
            flags |= ElementFlags.IsLeaf;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        var index = _metaDb.Append(
            ElementTokenType.PropertyName,
            parentRow: parentRow,
            operationReferenceId: selection.Id,
            operationReferenceType: OperationReferenceType.Selection,
            flags: flags);

        _metaDb.Append(
            ElementTokenType.None,
            parentRow: index);
    }

    private static void CheckExpectedType(ElementTokenType expected, ElementTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException();
        }
    }
}

internal static class MetaDbMemoryPool
{
    public static byte[] Rent() => new byte[ChunkSize];

    public static void Return(byte[] chunk)
    {
    }
}

internal static class JsonMemoryPool
{
    public static byte[] Rent() => new byte[ChunkSize];

    public static void Return(byte[] chunk)
    {
    }
}

internal static class MetaDbConstants
{
    // 6552 rows × 20 bytes
    public const int ChunkSize = RowsPerChunk * CompositeResultDocument.DbRow.Size;
    public const int RowsPerChunk = 6552;
}

internal readonly ref struct ValueRange(int location, int size)
{
    public int Location { get; } = location;
    public int Size { get; } = size;
}

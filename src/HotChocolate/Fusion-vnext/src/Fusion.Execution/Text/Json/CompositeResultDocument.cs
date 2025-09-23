using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;
using static HotChocolate.Fusion.Text.Json.MetaDbMemory;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly List<SourceResultDocument> _sources = [];
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private MetaDb _metaDb;
    private bool _disposed;

    public CompositeResultDocument(Operation operation, ulong includeFlags)
    {
        _metaDb = MetaDb.CreateForEstimatedRows(RowsPerChunk * 8);
        _operation =  operation;
        _includeFlags = includeFlags;

        // we create the root data object.
        Data = CreateObject(0, operation.RootSelectionSet);
    }

    public CompositeResultElement Data { get; }

    public CompositeResultElement Errors { get; } = default!;

    public CompositeResultElement Extensions { get; } = default!;

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
            if (selection.IsIncluded(_includeFlags))
            {
                WriteEmptyProperty(index, selection);
            }
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
        var parent = source._parent;

        if (parent.Id == -1)
        {
            Debug.Assert(
                !_sources.Contains(parent),
                "The source document is marked as unbound but is already registered.");

            parent.Id = _sources.Count;
            _sources.Add(parent);
        }

        Debug.Assert(
            _sources.Contains(parent),
            "Expected the source document of the source element to be registered.");

        _metaDb.Replace(
            index: target.Index,
            tokenType: source.TokenType.ToElementTokenType(),
            location: value.Location,
            sizeOrLength: value.Size,
            sourceDocumentId: parent.Id,
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

    public void Dispose()
    {
        if (!_disposed)
        {
            _metaDb.Dispose();
            _disposed = true;
        }
    }
}

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
        _operation = operation;
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
        if (index < 0)
        {
            return null;
        }

        var row = _metaDb.Get(index);

        if (row.TokenType is not ElementTokenType.PropertyName
            || row.OperationReferenceType is not OperationReferenceType.Selection)
        {
            return null;
        }

        return _operation.GetSelectionById(row.OperationReferenceId);
    }

    internal CompositeResultElement GetArrayIndexElement(int currentIndex, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var startIndex = _metaDb.GetStartIndex(currentIndex);
        var row = _metaDb.Get(startIndex);

        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        var arrayLength = row.NumberOfRows;

        if ((uint)arrayIndex >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        return new CompositeResultElement(this, startIndex + arrayIndex + 1);
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

    internal Path CreatePath(int currentIndex)
    {
        if (currentIndex == 0)
        {
            return Path.Root;
        }

        Span<int> indexes = stackalloc int[64];
        var index = currentIndex;
        var written = 0;

        do
        {
            indexes[written++] = index;
            index = _metaDb.GetParentIndex(index);

            if (written >= 64 && index > 0)
            {
                throw new InvalidOperationException("The path is to deep.");
            }
        } while (index > 0);

        var path = Path.Root;
        var parentTokenType = ElementTokenType.StartObject;

        indexes = indexes[..written];

        for (var i = indexes.Length - 1; i >= 1; i--)
        {
            index = indexes[i];
            var tokenType = _metaDb.GetElementTokenType(index, resolveReferences: false);

            if (tokenType == ElementTokenType.PropertyName)
            {
                path = path.Append(GetSelection(index)!.ResponseName);
                // we jump over the actual value.
                i--;
            }
            else if (indexes.Length - 1 > i)
            {
                var parentIndex = indexes[i + 1];
                if (parentTokenType is ElementTokenType.StartArray)
                {
                    var parentRow = _metaDb.Get(parentIndex);
                    var arrayIndex = (parentIndex + parentRow.SizeOrLength) - index;
                    path = path.Append(arrayIndex);
                }
            }

            parentTokenType = tokenType;
        }

        return path;
    }

    internal CompositeResultElement GetParent(int currentIndex)
    {
        var flags = _metaDb.GetFlags(currentIndex);

        if ((flags & ElementFlags.IsRoot) == ElementFlags.IsRoot)
        {
            return default;
        }

        var parentIndex = _metaDb.GetParentIndex(currentIndex);
        return new CompositeResultElement(this, parentIndex);
    }

    internal bool IsInvalidated(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var elementFlags = _metaDb.GetElementTokenType(currentIndex);

        if (elementFlags is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(currentIndex);
            return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (elementFlags is ElementTokenType.Reference)
        {
            currentIndex = _metaDb.GetLocation(currentIndex);
            elementFlags = _metaDb.GetElementTokenType(currentIndex);

            if (elementFlags is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(currentIndex);
                return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
            }
        }

        return false;
    }

    internal bool IsNullOrInvalidated(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var elementFlags = _metaDb.GetElementTokenType(currentIndex);

        if (elementFlags is ElementTokenType.Null)
        {
            return true;
        }

        if (elementFlags is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(currentIndex);
            return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (elementFlags is ElementTokenType.Reference)
        {
            currentIndex = _metaDb.GetLocation(currentIndex);
            elementFlags = _metaDb.GetElementTokenType(currentIndex);

            if (elementFlags is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(currentIndex);
                return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
            }
        }

        return false;
    }

    internal bool IsInternalProperty(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (currentIndex == 0)
        {
            return false;
        }

        currentIndex--;
        var flags = _metaDb.GetFlags(currentIndex);
        return (flags & ElementFlags.IsInternal) == ElementFlags.IsInternal;
    }

    internal void Invalidate(int currentIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var elementFlags = _metaDb.GetElementTokenType(currentIndex);

        if (elementFlags is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(currentIndex);
            _metaDb.SetFlags(currentIndex, flags | ElementFlags.Invalidated);
            return;
        }

        if (elementFlags is ElementTokenType.Reference)
        {
            currentIndex = _metaDb.GetLocation(currentIndex);
            elementFlags = _metaDb.GetElementTokenType(currentIndex);

            if (elementFlags is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(currentIndex);
                _metaDb.SetFlags(currentIndex, flags | ElementFlags.Invalidated);
                return;
            }
        }

        throw new InvalidOperationException("Only objects can be invalidated.");
    }

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
    {
        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.OperationReferenceId).RawResponseName;
        }

        if ((row.Flags & ElementFlags.SourceResult) == ElementFlags.SourceResult)
        {
            var document = _sources[row.SourceDocumentId];
            return document.ReadRawValue(row.Location, row.SizeOrLength);
        }

        throw new NotImplementedException();
    }

    private ReadOnlyMemory<byte> ReadRawValueAsMemory(DbRow row)
    {
        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.OperationReferenceId).RawResponseNameAsMemory;
        }

        if ((row.Flags & ElementFlags.SourceResult) == ElementFlags.SourceResult)
        {
            var document = _sources[row.SourceDocumentId];
            return document.ReadRawValueAsMemory(row.Location, row.SizeOrLength);
        }

        throw new NotSupportedException();
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

    internal CompositeResultElement CreateArray(int parentRow, int length)
    {
        var index = WriteStartArray(parentRow, length);

        for (var i = 0; i < length; i++)
        {
            WriteEmptyValue(index);
        }

        WriteEndArray();

        return new CompositeResultElement(this, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignCompositeValue(CompositeResultElement target, CompositeResultElement value)
    {
        _metaDb.Replace(
            index: target.Index,
            tokenType: ElementTokenType.Reference,
            location: value.Index,
            parentRow: _metaDb.GetParentIndex(target.Index));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignSourceValue(CompositeResultElement target, SourceResultElement source)
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
            parentRow: _metaDb.GetParentIndex(target.Index),
            flags: ElementFlags.SourceResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNullValue(CompositeResultElement target)
    {
        _metaDb.Replace(
            index: target.Index,
            tokenType: ElementTokenType.Null,
            parentRow: _metaDb.GetParentIndex(target.Index));
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
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndObject() => _metaDb.Append(ElementTokenType.EndObject);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int WriteStartArray(int parentRow = 0, int length = 0)
    {
        var flags = ElementFlags.None;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.Append(
            ElementTokenType.StartArray,
            sizeOrLength: length,
            parentRow: parentRow,
            numberOfRows: (length * 2) + 2,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndArray() => _metaDb.Append(ElementTokenType.EndArray);

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyValue(int parentRow)
    {
        _metaDb.Append(
            ElementTokenType.None,
            parentRow: parentRow);
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

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Text.Json;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly List<SourceResultDocument> _sources = [];
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private readonly ulong _deferFlags;
    private readonly PathSegmentLocalPool? _pathPool;
    internal MetaDb _metaDb;
    private bool _disposed;

    internal CompositeResultDocument(
        Operation operation,
        ulong includeFlags,
        ulong deferFlags = 0,
        PathSegmentLocalPool? pathPool = null)
    {
        _metaDb = MetaDb.CreateForEstimatedRows(Cursor.RowsPerChunk * 8);
        _operation = operation;
        _includeFlags = includeFlags;
        _deferFlags = deferFlags;
        _pathPool = pathPool;

        Data = CreateObject(Cursor.Zero, operation.RootSelectionSet);
    }

    public CompositeResultElement Data { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ElementTokenType GetElementTokenType(Cursor cursor)
        => _metaDb.GetElementTokenType(cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Operation GetOperation()
        => _operation;

    internal SelectionSet? GetSelectionSet(Cursor cursor)
    {
        var row = _metaDb.Get(cursor);

        if (row.OperationReferenceType is not OperationReferenceType.SelectionSet)
        {
            return null;
        }

        return _operation.GetSelectionSetById(row.OperationReferenceId);
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

        if (row.OperationReferenceType is not OperationReferenceType.Selection)
        {
            return null;
        }

        return _operation.GetSelectionById(row.OperationReferenceId);
    }

    internal CompositeResultElement GetArrayIndexElement(Cursor current, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        if ((uint)arrayIndex >= (uint)row.NumberOfRows)
        {
            throw new IndexOutOfRangeException();
        }

        // first element is at +1 after StartArray
        return new CompositeResultElement(this, current.AddRows(arrayIndex + 1));
    }

    internal int GetArrayLength(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        return row.SizeOrLength;
    }

    internal int GetPropertyCount(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    internal CompactPath CreateCompactPath(Cursor current)
    {
        var firstRow = _metaDb.Get(current);

        // Stop at root via IsRoot flag.
        if ((firstRow.Flags & ElementFlags.IsRoot) == ElementFlags.IsRoot)
        {
            return CompactPath.Root;
        }

        Span<Cursor> chain = stackalloc Cursor[64];
        Span<DbRow> rows = stackalloc DbRow[64];
        chain[0] = current;
        rows[0] = firstRow;
        var written = 1;

        var parentIndex = firstRow.ParentRow;
        while (parentIndex > 0)
        {
            var cursor = Cursor.FromIndex(parentIndex);
            var row = _metaDb.Get(cursor);
            chain[written] = cursor;
            rows[written] = row;
            written++;

            parentIndex = row.ParentRow;

            if (written >= 64)
            {
                throw new InvalidOperationException("The path is to deep.");
            }
        }

        Span<int> pathBuffer = stackalloc int[32];
        var path = new CompactPathBuilder(pathBuffer, _pathPool);
        var parentTokenType = ElementTokenType.StartObject;

        for (var i = written - 1; i >= 0; i--)
        {
            var cursor = chain[i];
            var tokenType = rows[i].TokenType;

            if (tokenType == ElementTokenType.PropertyName)
            {
                Debug.Assert(rows[i].OperationReferenceType is OperationReferenceType.Selection);
                path.AppendField(rows[i].OperationReferenceId);
                i--; // skip over the actual value
            }
            else if (written - 1 > i)
            {
                var parentCursor = chain[i + 1];

                if (parentTokenType is ElementTokenType.StartArray)
                {
                    // arrayIndex = abs(child) - (abs(parent) + 1)
                    var absChild = (cursor.Chunk * Cursor.RowsPerChunk) + cursor.Row;
                    var absParent = (parentCursor.Chunk * Cursor.RowsPerChunk) + parentCursor.Row;
                    var arrayIndex = absChild - (absParent + 1);
                    path.AppendIndex(arrayIndex);
                }
            }

            parentTokenType = tokenType;
        }

        return path.ToPath();
    }

    internal Path CreatePath(Cursor current)
        => CreateCompactPath(current).ToPath(_operation);

    internal CompositeResultElement GetParent(Cursor current)
    {
        // The null cursor represents the data object, which is the utmost root.
        // If we have reached that we simply return an undefined element
        if (current == Cursor.Zero)
        {
            return default;
        }

        var parent = _metaDb.GetParentCursor(current);
        var parentRow = _metaDb.Get(parent);

        // if the parent element is a property name then we must get the parent of that,
        // as property name and value represent the same element.
        if (parentRow.TokenType is ElementTokenType.PropertyName)
        {
            parent = Cursor.FromIndex(parentRow.ParentRow);
            parentRow = _metaDb.Get(parent);
        }

        // if we have not yet reached the root and the element type of the parent is an object or an array
        // then we need to get still the parent of this row as we want to get the logical parent
        // which is the value level of the property or the element in an array.
        if (parent != Cursor.Zero
            && parentRow.TokenType is ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            parent = Cursor.FromIndex(parentRow.ParentRow);

            // in this case the parent must be a reference, otherwise we would have
            // found an inconsistency in the database.
            Debug.Assert(_metaDb.Get(parent).TokenType == ElementTokenType.Reference);
        }

        return new CompositeResultElement(this, parent);
    }

    internal bool IsInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.StartObject)
        {
            return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (row.TokenType is ElementTokenType.Reference)
        {
            row = _metaDb.Get(Cursor.FromIndex(row.Location));

            if (row.TokenType is ElementTokenType.StartObject)
            {
                return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
            }
        }

        return false;
    }

    internal bool IsNullOrInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.Null)
        {
            return true;
        }

        if (row.TokenType is ElementTokenType.Reference)
        {
            row = _metaDb.Get(Cursor.FromIndex(row.Location));
        }

        if (row.TokenType is ElementTokenType.StartObject)
        {
            return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
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

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.Reference)
        {
            current = Cursor.FromIndex(row.Location);
            row = _metaDb.Get(current);
        }

        if (row.TokenType is ElementTokenType.None or ElementTokenType.StartArray)
        {
            return;
        }

        if (row.TokenType is ElementTokenType.StartObject)
        {
            _metaDb.SetFlags(current, row.Flags | ElementFlags.Invalidated);
            return;
        }

        Debug.Fail("Only objects can be invalidated.");
    }

    private ReadOnlySpan<byte> ReadRawValue(DbRow row)
    {
        if (row.TokenType == ElementTokenType.Null)
        {
            return JsonConstants.NullValue;
        }

        if (row.TokenType == ElementTokenType.True)
        {
            return JsonConstants.TrueValue;
        }

        if (row.TokenType == ElementTokenType.False)
        {
            return JsonConstants.FalseValue;
        }

        if (row.TokenType == ElementTokenType.PropertyName)
        {
            return _operation.GetSelectionById(row.OperationReferenceId).Utf8ResponseName;
        }

        if ((row.Flags & ElementFlags.SourceResult) == ElementFlags.SourceResult)
        {
            var document = _sources[row.SourceDocumentId];
            return document.ReadRawValue(row.Location, row.SizeOrLength);
        }

        throw new NotSupportedException();
    }

    internal CompositeResultElement CreateObject(Cursor parent, SelectionSet selectionSet)
    {
        var selections = selectionSet.Selections;
        var startObjectCursor = WriteStartObject(parent, selectionSet.Id, selections.Length);

        foreach (var selection in selections)
        {
            WriteEmptyProperty(startObjectCursor, selection);
        }

        _metaDb.AppendEndObject();

        return new CompositeResultElement(this, startObjectCursor);
    }

    internal CompositeResultElement CreateArray(Cursor parent, int length)
    {
        var cursor = WriteStartArray(parent, length);

        _metaDb.AppendNullRange(cursor.Index, length);

        WriteEndArray();

        return new CompositeResultElement(this, cursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignCompositeValue(CompositeResultElement target, CompositeResultElement value)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Reference,
            location: value.Cursor.Index,
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignSourceValue(CompositeResultElement target, SourceResultElement source)
    {
        var value = source.GetValuePointer();
        var parent = source._parent;

        if (parent.Id == -1)
        {
            Debug.Assert(!_sources.Contains(parent), "The source document is marked as unbound but is already registered.");
            parent.Id = _sources.Count;
            _sources.Add(parent);
        }

        Debug.Assert(_sources.Contains(parent), "Expected the source document of the source element to be registered.");

        var tokenType = source.TokenType.ToElementTokenType();

        if (tokenType is ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            var sourceCursor = source._cursor;

            _metaDb.Replace(
                cursor: target.Cursor,
                tokenType: source.TokenType.ToElementTokenType(),
                location: sourceCursor.Chunk,
                sizeOrLength: sourceCursor.Row,
                sourceDocumentId: parent.Id,
                parentRow: _metaDb.GetParent(target.Cursor),
                flags: ElementFlags.SourceResult);
            return;
        }

        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: source.TokenType.ToElementTokenType(),
            location: value.Location,
            sizeOrLength: value.Size,
            sourceDocumentId: parent.Id,
            parentRow: _metaDb.GetParent(target.Cursor),
            flags: ElementFlags.SourceResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNullValue(CompositeResultElement target)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Null,
            parentRow: _metaDb.GetParent(target.Cursor));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartObject(Cursor parent, int selectionSetId, int propertyCount)
    {
        var flags = ElementFlags.None;
        var parentRow = parent.Index;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.AppendStartObject(parentRow, selectionSetId, propertyCount, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartArray(Cursor parent, int length = 0)
    {
        var flags = ElementFlags.None;
        var parentRow = parent.Index;

        if (parentRow < 0)
        {
            parentRow = 0;
            flags = ElementFlags.IsRoot;
        }

        return _metaDb.AppendStartArray(parentRow, length, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndArray() => _metaDb.AppendEndArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyProperty(Cursor parent, Selection selection)
    {
        var flags = ElementFlags.None;

        if (selection.IsInternal)
        {
            flags = ElementFlags.IsInternal;
        }

        if (!selection.IsIncluded(_includeFlags) || selection.IsDeferred(_deferFlags))
        {
            flags |= ElementFlags.IsExcluded;
        }

        if (selection.Type.Kind is not TypeKind.NonNull)
        {
            flags |= ElementFlags.IsNullable;
        }

        _metaDb.AppendEmptyPropertyWithNullValue(
            parentRow: parent.Index,
            selectionId: selection.Id,
            flags: flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyValue(Cursor parent) => _metaDb.AppendNull(parent.Index);

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
            _disposed = true;
        }
    }
}

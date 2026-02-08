using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
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
    internal MetaDb _metaDb;
    private bool _disposed;

    public CompositeResultDocument(Operation operation, ulong includeFlags)
    {
        _metaDb = MetaDb.CreateForEstimatedRows(Cursor.RowsPerChunk * 8);
        _operation = operation;
        _includeFlags = includeFlags;

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

        var (start, tokenType) = _metaDb.GetStartCursor(current);

        CheckExpectedType(ElementTokenType.StartArray, tokenType);

        var len = _metaDb.GetNumberOfRows(start);

        if ((uint)arrayIndex >= (uint)len)
        {
            throw new IndexOutOfRangeException();
        }

        // first element is at +1 after StartArray
        return new CompositeResultElement(this, start.AddRows(arrayIndex + 1));
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
            return Path.Root;
        }

        Span<Cursor> chain = stackalloc Cursor[64];
        var c = current;
        var written = 0;

        do
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
        } while (true);

        var path = Path.Root;
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
                    var absChild = c.Chunk * Cursor.RowsPerChunk + c.Row;
                    var absParent = parentCursor.Chunk * Cursor.RowsPerChunk + parentCursor.Row;
                    var arrayIndex = absChild - (absParent + 1);
                    path = path.Append(arrayIndex);
                }
            }

            parentTokenType = tokenType;
        }

        return path;
    }

    internal CompositeResultElement GetParent(Cursor current)
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

        return new CompositeResultElement(this, parent);
    }

    internal bool IsInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tokenType = _metaDb.GetElementTokenType(current, resolveReferences: false);

        if (tokenType is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(current);
            return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
            }
        }

        return false;
    }

    internal bool IsNullOrInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var tokenType = _metaDb.GetElementTokenType(current);

        if (tokenType is ElementTokenType.Null)
        {
            return true;
        }

        if (tokenType is ElementTokenType.StartObject)
        {
            var flags = _metaDb.GetFlags(current);
            return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                return (flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
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
            _metaDb.SetFlags(current, flags | ElementFlags.Invalidated);
            return;
        }

        if (tokenType is ElementTokenType.Reference)
        {
            current = _metaDb.GetLocationCursor(current);
            tokenType = _metaDb.GetElementTokenType(current);

            if (tokenType is ElementTokenType.StartObject)
            {
                var flags = _metaDb.GetFlags(current);
                _metaDb.SetFlags(current, flags | ElementFlags.Invalidated);
            }

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
        var startObjectCursor = WriteStartObject(parent, selectionSet.Id);

        var selectionCount = 0;
        foreach (var selection in selectionSet.Selections)
        {
            WriteEmptyProperty(startObjectCursor, selection);
            selectionCount++;
        }

        WriteEndObject(startObjectCursor, selectionCount);

        return new CompositeResultElement(this, startObjectCursor);
    }

    internal CompositeResultElement CreateArray(Cursor parent, int length)
    {
        var cursor = WriteStartArray(parent, length);

        for (var i = 0; i < length; i++)
        {
            WriteEmptyValue(cursor);
        }

        WriteEndArray();

        return new CompositeResultElement(this, cursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignCompositeValue(CompositeResultElement target, CompositeResultElement value)
    {
        _metaDb.Replace(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Reference,
            location: value.Cursor.ToIndex(),
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
    private Cursor WriteStartObject(Cursor parent, int selectionSetId = 0)
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
            operationReferenceType: OperationReferenceType.SelectionSet,
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
    private void WriteEmptyProperty(Cursor parent, Selection selection)
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
            _disposed = true;
        }
    }
}

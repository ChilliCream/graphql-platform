using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

public sealed partial class CompositeResultDocument : IDisposable
{
    private static readonly Encoding s_utf8Encoding = Encoding.UTF8;
    private readonly List<SourceResultDocument> _sources = [];
    private readonly Operation _operation;
    private readonly ulong _includeFlags;
    private readonly ulong _deferFlags;
    private readonly ulong[]? _wideIncludeFlags;
    private readonly ulong[]? _wideDeferFlags;
    private PathSegmentLocalPool? _pathPool;
    internal MetaDb _metaDb;
    private NullMarkerState _nullMarkerState;
    private int _layoutVersion;
    private int _shapeVersion;
    private int _disposed;

    internal CompositeResultDocument(
        IMemoryArena arena,
        Operation operation,
        ulong includeFlags,
        ulong deferFlags = 0,
        PathSegmentLocalPool? pathPool = null,
        ulong[]? wideIncludeFlags = null,
        ulong[]? wideDeferFlags = null)
        : this(
            arena,
            operation,
            new ConditionFlags(includeFlags, wideIncludeFlags),
            new ConditionFlags(deferFlags, wideDeferFlags),
            pathPool)
    {
    }

    internal CompositeResultDocument(
        IMemoryArena arena,
        Operation operation,
        ConditionFlags includeFlags,
        ConditionFlags deferFlags = default,
        PathSegmentLocalPool? pathPool = null)
    {
        ArgumentNullException.ThrowIfNull(arena);

        var zero = Cursor.CreateZero();

        _metaDb = MetaDb.Create(arena);
        _operation = operation;
        _includeFlags = includeFlags.Word0;
        _deferFlags = deferFlags.Word0;
        _wideIncludeFlags = includeFlags.Overflow;
        _wideDeferFlags = deferFlags.Overflow;
        _pathPool = pathPool;

        Data = CreateObject(zero, operation.RootSelectionSet);
    }

    public CompositeResultElement Data { get; }

    /// <summary>
    /// Gets a version that changes whenever a value that references an object or a list is
    /// replaced. While it is unchanged, every element keeps the path it had when it was read.
    /// </summary>
    internal int LayoutVersion => _layoutVersion;

    /// <summary>
    /// Gets a version that changes whenever an object or a list is assigned to a value or a value
    /// that references an object or a list is replaced. While it is unchanged, walking the same
    /// field path from the root reaches the same objects.
    /// </summary>
    internal int ShapeVersion => _shapeVersion;

    /// <summary>
    /// Stops this document from renting path segment arrays from the request scoped pool.
    /// Paths created afterwards are allocated.
    /// </summary>
    internal void DetachPathPool() => Volatile.Write(ref _pathPool, null);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ElementTokenType GetElementTokenType(Cursor cursor)
        => _metaDb.GetElementTokenType(cursor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal Operation GetOperation()
        => _operation;

    internal SelectionSet? GetSelectionSet(Cursor cursor)
    {
        var row = _metaDb.Get(cursor);

        if (row.TokenType is ElementTokenType.Reference)
        {
            row = _metaDb.Get(new Cursor(row.Location));
        }

        if (row.OperationReferenceType is not OperationReferenceType.SelectionSet)
        {
            return null;
        }

        return _operation.GetSelectionSetById(row.OperationReferenceId);
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

        if (row.OperationReferenceType is not OperationReferenceType.Selection)
        {
            return null;
        }

        return _operation.GetSelectionById(row.OperationReferenceId);
    }

    internal CompositeResultElement GetArrayIndexElement(Cursor current, int arrayIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        if ((uint)arrayIndex >= (uint)row.NumberOfRows)
        {
            throw new IndexOutOfRangeException(
                $"Array index {arrayIndex} is out of range for an array of length {row.NumberOfRows}.");
        }

        // first element is at +1 after StartArray
        return new CompositeResultElement(this, current.AddRows(arrayIndex + 1));
    }

    internal int GetArrayLength(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartArray, row.TokenType);

        return row.SizeOrLength;
    }

    internal int GetPropertyCount(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.GetValue(ref current);
        CheckExpectedType(ElementTokenType.StartObject, row.TokenType);

        return row.SizeOrLength;
    }

    /// <summary>
    /// Creates the path of the element at <paramref name="current"/>. When <paramref name="pooled"/>
    /// is <c>false</c>, the path arrays are allocated instead of rented from the request scoped pool.
    /// </summary>
    [SkipLocalsInit]
    internal CompactPath CreateCompactPath(Cursor current, bool pooled = true)
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

        var parentCursor = new Cursor(firstRow.Parent);
        while (!parentCursor.IsZero)
        {
            var row = _metaDb.Get(parentCursor);
            chain[written] = parentCursor;
            rows[written] = row;
            written++;

            parentCursor = new Cursor(row.Parent);

            if (written >= 64)
            {
                throw new InvalidOperationException("The path is to deep.");
            }
        }

        Span<int> pathBuffer = stackalloc int[32];
        var path = new CompactPathBuilder(pathBuffer, pooled ? Volatile.Read(ref _pathPool) : null);
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
                var arrayParentCursor = chain[i + 1];

                if (parentTokenType is ElementTokenType.StartArray)
                {
                    // arrayIndex = abs(child) - (abs(parent) + 1)
                    var arrayIndex = cursor.Index - (arrayParentCursor.Index + 1);
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
        if (current.IsZero)
        {
            return default;
        }

        var parent = _metaDb.GetParentCursor(current);
        var parentRow = _metaDb.Get(parent);

        // if the parent element is a property name then we must get the parent of that,
        // as property name and value represent the same element.
        if (parentRow.TokenType is ElementTokenType.PropertyName)
        {
            parent = new Cursor(parentRow.Parent);
            parentRow = _metaDb.Get(parent);
        }

        // if we have not yet reached the root and the element type of the parent is an object or an array
        // then we need to get still the parent of this row as we want to get the logical parent
        // which is the value level of the property or the element in an array.
        if (!parent.IsZero
            && parentRow.TokenType is ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            parent = new Cursor(parentRow.Parent);

            // in this case the parent must be a reference, otherwise we would have
            // found an inconsistency in the database.
            Debug.Assert(_metaDb.Get(parent).TokenType == ElementTokenType.Reference);
        }

        return new CompositeResultElement(this, parent);
    }

    internal bool IsInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.StartObject)
        {
            return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        if (row.TokenType is ElementTokenType.Reference)
        {
            row = _metaDb.Get(new Cursor(row.Location));

            if (row.TokenType is ElementTokenType.StartObject)
            {
                return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
            }
        }

        return false;
    }

    internal bool IsNullOrInvalidated(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.Null)
        {
            return true;
        }

        if (row.TokenType is ElementTokenType.Reference)
        {
            row = _metaDb.Get(new Cursor(row.Location));
        }

        if (row.TokenType is ElementTokenType.StartObject)
        {
            return (row.Flags & ElementFlags.Invalidated) == ElementFlags.Invalidated;
        }

        return false;
    }

    internal bool RequiresNullMarkerFinalization
        => (_nullMarkerState & NullMarkerState.RequiresFinalization)
            is NullMarkerState.RequiresFinalization;

    internal bool HasNullMarkers
        => (_nullMarkerState & NullMarkerState.HasMarkers) is NullMarkerState.HasMarkers;

    internal bool IsNullMarker(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        return (_metaDb.GetFlags(current) & ElementFlags.NullMarker) is ElementFlags.NullMarker;
    }

    internal void RequireNullMarkerFinalization()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        _nullMarkerState |= NullMarkerState.RequiresFinalization;
    }

    internal void CompleteNullMarkerFinalization()
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        _nullMarkerState &= ~NullMarkerState.RequiresFinalization;
    }

    internal void SetNullMarker(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var flags = _metaDb.GetFlags(current);
        if ((flags & ElementFlags.NullMarker) is not ElementFlags.NullMarker)
        {
            _metaDb.SetFlags(current, flags | ElementFlags.NullMarker);
        }

        _nullMarkerState |= NullMarkerState.HasMarkers;
    }

    [Flags]
    private enum NullMarkerState : byte
    {
        None = 0,
        RequiresFinalization = 1,
        HasMarkers = 2
    }

    internal bool IsInternalProperty(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        // The flag sits on the property row (one before value)
        var propertyCursor = current.AddRows(-1);
        var flags = _metaDb.GetFlags(propertyCursor);
        return (flags & ElementFlags.IsInternal) == ElementFlags.IsInternal;
    }

    internal bool IsEnumValueProperty(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        // The flag sits on the property row (one before value)
        var propertyCursor = current.AddRows(-1);
        var flags = _metaDb.GetFlags(propertyCursor);
        return (flags & ElementFlags.IsEnumValue) == ElementFlags.IsEnumValue;
    }

    internal void Invalidate(Cursor current)
    {
        ObjectDisposedException.ThrowIf(_disposed != 0, this);

        var row = _metaDb.Get(current);

        if (row.TokenType is ElementTokenType.Reference)
        {
            current = new Cursor(row.Location);
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

        if (row.TokenType == ElementTokenType.String)
        {
            // An inline string value synthesized by the result layer (the __typename
            // introspection field). Its concrete type comes from the enclosing selection set,
            // which the merge has already specialized to the runtime type.
            var typeName = _operation.GetSelectionSetById(row.Location).Type.Name;
            return Utf8StringCache.GetJsonPropertyName(typeName).AsSpan(..^1);
        }

        throw new NotSupportedException();
    }

    internal CompositeResultElement CreateObject(Cursor parent, SelectionSet selectionSet)
        => CreateObject(parent, selectionSet, out _);

    internal CompositeResultElement CreateObject(
        Cursor parent,
        SelectionSet selectionSet,
        out CompositeObjectContext objectContext)
    {
        var template = selectionSet.ObjectTemplate;
        Debug.Assert(template.Rows.Length > 0, "The selection set is in an invalid state.");

        var selections = selectionSet.Selections;

        var startObjectCursor = template.ConditionalSelections.IsEmpty
            ? _metaDb.AppendObjectBlock(template.Rows, parent.Value, excludedRowOffsets: [])
            : AppendConditionalObjectBlock(parent, template, selections);

        objectContext = new CompositeObjectContext(
            this,
            startObjectCursor,
            selectionSet,
            (selections.Length * 2) + 1);

        return new CompositeResultElement(this, startObjectCursor);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private Cursor AppendConditionalObjectBlock(
        Cursor parent,
        ObjectTemplate template,
        ReadOnlySpan<Selection> selections)
    {
        // The template rows are canonical: they never carry IsExcluded. Each document evaluates
        // its own skip/include and defer state and hands the block-relative property row offsets
        // of the selections it excludes to the block append, which stamps the flag alongside the
        // parent pointers.
        var conditionalSelections = template.ConditionalSelections;
        var excludedRowOffsets = conditionalSelections.Length <= 256
            ? stackalloc int[conditionalSelections.Length]
            : new int[conditionalSelections.Length];
        var excludedCount = 0;

        if (_wideIncludeFlags is null && _wideDeferFlags is null)
        {
            foreach (var index in conditionalSelections)
            {
                if (IsExcluded(selections[index]))
                {
                    // The property row of selection index i is row (i * 2) + 1 of the object block.
                    excludedRowOffsets[excludedCount++] = (index * 2) + 1;
                }
            }
        }
        else
        {
            foreach (var index in conditionalSelections)
            {
                if (IsExcludedWide(selections[index]))
                {
                    // The property row of selection index i is row (i * 2) + 1 of the object block.
                    excludedRowOffsets[excludedCount++] = (index * 2) + 1;
                }
            }
        }

        return _metaDb.AppendObjectBlock(
            template.Rows,
            parent.Value,
            excludedRowOffsets[..excludedCount]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsExcluded(Selection selection)
        => !selection.IsIncludedUnchecked(_includeFlags)
            || selection.IsDeferredUnchecked(_deferFlags);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsExcludedWide(Selection selection)
        => !selection.IsIncludedWide(_includeFlags, _wideIncludeFlags)
            || selection.IsDeferredWide(_deferFlags, _wideDeferFlags);

    /// <summary>
    /// Upgrades an interface-typed element produced by an <c>@interfaceObject</c> stand-in to its
    /// recovered concrete type. A new object is created with the concrete selection set, the
    /// interface-declared fields already completed on the old object are carried over by response
    /// name, and <paramref name="element"/> is re-pointed at the concrete object so the covering
    /// lookup can complete the identity-dependent fields.
    /// </summary>
    internal void UpgradeObject(CompositeResultElement element, SelectionSet concreteSelectionSet)
    {
        var newObject = CreateObject(element.Cursor, concreteSelectionSet);

        foreach (var property in element.EnumerateObject())
        {
            var selection = property.Selection;

            // __typename is synthesized from the concrete selection set; the interface-named
            // placeholder on the opaque object must never overwrite it.
            if (selection is null || selection.Field.IsIntrospectionField)
            {
                continue;
            }

            if (newObject.TryGetProperty(selection.Utf8ResponseName, out var slot))
            {
                AssignCompositeValue(slot, property.Value);
            }
        }

        AssignCompositeValue(element, newObject);
    }

    internal CompositeResultElement CreateArray(Cursor parent, int length)
    {
        var cursor = WriteStartArray(parent, length);

        _metaDb.AppendNullRange(cursor.Value, length);

        WriteEndArray();

        return new CompositeResultElement(this, cursor);
    }

    /// <summary>
    /// Creates an object for <paramref name="selectionSet"/> and assigns it to the undefined
    /// slot at <paramref name="target"/>.
    /// </summary>
    internal void InitializeObject(
        Cursor target,
        SelectionSet selectionSet,
        out CompositeObjectContext objectContext)
    {
        Debug.Assert(
            _metaDb.GetElementTokenType(target) is ElementTokenType.None,
            "Only an undefined slot can be initialized.");

        var obj = CreateObject(target, selectionSet, out objectContext);
        _shapeVersion++;

        _metaDb.ReplacePreserveParent(
            cursor: target,
            tokenType: ElementTokenType.Reference,
            location: obj.Cursor.Value);
    }

    /// <summary>
    /// Creates an array of <paramref name="length"/> undefined elements and assigns it to the
    /// undefined slot at <paramref name="target"/>.
    /// </summary>
    /// <returns>
    /// An enumerator over the elements of the new array.
    /// </returns>
    internal CompositeResultElement.ArrayEnumerator InitializeArray(Cursor target, int length)
    {
        Debug.Assert(
            _metaDb.GetElementTokenType(target) is ElementTokenType.None,
            "Only an undefined slot can be initialized.");

        var array = CreateArray(target, length);
        _shapeVersion++;

        _metaDb.ReplacePreserveParent(
            cursor: target,
            tokenType: ElementTokenType.Reference,
            location: array.Cursor.Value);

        return new CompositeResultElement.ArrayEnumerator(this, array.Cursor, length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignCompositeValue(CompositeResultElement target, CompositeResultElement value)
    {
        TrackValueReplacement(target.Cursor);
        _shapeVersion++;
        _metaDb.ReplacePreserveParent(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Reference,
            location: value.Cursor.Value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignSourceValue(CompositeResultElement target, SourceResultElement source)
        => AssignSourceValue(target, source._parent, source._cursor, source.GetValueRow());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignSourceValue(
        CompositeResultElement target,
        SourceResultDocument source,
        SourceResultDocument.Cursor sourceCursor,
        SourceResultDocument.DbRow sourceRow)
    {
        var parent = source;

        if (parent.Id == -1)
        {
            Debug.Assert(!_sources.Contains(parent), "The source document is marked as unbound but is already registered.");
            parent.Id = _sources.Count;
            _sources.Add(parent);
        }

        Debug.Assert(_sources.Contains(parent), "Expected the source document of the source element to be registered.");

        var tokenType = sourceRow.TokenType.ToElementTokenType();
        TrackValueReplacement(target.Cursor);

        if (tokenType is ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            _shapeVersion++;
            _metaDb.ReplacePreserveParent(
                cursor: target.Cursor,
                tokenType: tokenType,
                location: sourceCursor.Chunk,
                sizeOrLength: sourceCursor.Row,
                sourceDocumentId: parent.Id,
                flags: ElementFlags.SourceResult);
            return;
        }

        _metaDb.ReplacePreserveParent(
            cursor: target.Cursor,
            tokenType: tokenType,
            location: sourceRow.Location,
            sizeOrLength: sourceRow.SizeOrLength,
            sourceDocumentId: parent.Id,
            flags: ElementFlags.SourceResult);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AssignNullValue(CompositeResultElement target)
    {
        TrackValueReplacement(target.Cursor);
        _metaDb.ReplacePreserveParent(
            cursor: target.Cursor,
            tokenType: ElementTokenType.Null);
    }

    /// <summary>
    /// Assigns the leaf value of <paramref name="source"/> to <paramref name="target"/> without
    /// copying the value data. Both elements must belong to this document.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the source holds a non-null leaf value that was assigned;
    /// otherwise, <c>false</c> and the target is unchanged.
    /// </returns>
    internal bool TryAssignLeafValue(CompositeResultElement target, CompositeResultElement source)
        => TryAssignLeafValue(target.Cursor, source.Cursor);

    /// <summary>
    /// Assigns the leaf value at <paramref name="source"/> to the slot at <paramref name="target"/>
    /// without copying the value data.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the source holds a non-null leaf value that was assigned;
    /// otherwise, <c>false</c> and the target is unchanged.
    /// </returns>
    internal bool TryAssignLeafValue(Cursor target, Cursor source)
    {
        var row = _metaDb.Get(source);

        if (!IsAssignableLeafValue(row))
        {
            return false;
        }

        TrackValueReplacement(target);
        _metaDb.ReplacePreserveParent(
            cursor: target,
            tokenType: row.TokenType,
            location: row.Location,
            sizeOrLength: row.SizeOrLength,
            sourceDocumentId: row.SourceDocumentId,
            flags: row.Flags);
        return true;
    }

    /// <summary>
    /// Classifies the slot at <paramref name="cursor"/> for
    /// <see cref="TryAssignLeafValue(Cursor, Cursor)"/>.
    /// </summary>
    internal LeafValueState GetLeafValueState(Cursor cursor)
    {
        var row = _metaDb.Get(cursor);

        if (IsAssignableLeafValue(row))
        {
            return LeafValueState.Assignable;
        }

        return row.TokenType is ElementTokenType.Null
            or ElementTokenType.None
            or ElementTokenType.StartObject
            or ElementTokenType.StartArray
            ? LeafValueState.NotALeaf
            : LeafValueState.Unsupported;
    }

    private static bool IsAssignableLeafValue(DbRow row)
        => (row.TokenType is ElementTokenType.String
                or ElementTokenType.Number
                or ElementTokenType.True
                or ElementTokenType.False)
            && (row.Flags & ElementFlags.Invalidated) != ElementFlags.Invalidated;

    /// <summary>
    /// Reads the token type and raw bytes of the value at <paramref name="cursor"/> for a key
    /// comparison. Only values that serialize as their raw bytes are readable: source result
    /// strings and numbers, booleans, null and an unset slot (<see cref="ElementTokenType.None"/>).
    /// </summary>
    /// <returns>
    /// <c>true</c> if the value is readable; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetRawLeafValue(Cursor cursor, out ElementTokenType tokenType, out ReadOnlySpan<byte> value)
    {
        var row = _metaDb.Get(cursor);
        tokenType = row.TokenType;

        switch (tokenType)
        {
            case ElementTokenType.String or ElementTokenType.Number
                when (row.Flags & ElementFlags.SourceResult) == ElementFlags.SourceResult:
                value = _sources[row.SourceDocumentId].ReadRawValue(row.Location, row.SizeOrLength);
                return true;

            case ElementTokenType.True
                or ElementTokenType.False
                or ElementTokenType.Null
                or ElementTokenType.None:
                value = default;
                return true;

            default:
                value = default;
                return false;
        }
    }

    /// <summary>
    /// Resolves the object at <paramref name="cursor"/> to the first row of an object that a
    /// selection set describes.
    /// </summary>
    /// <returns>
    /// <c>true</c> if the cursor refers to an object with a selection set; otherwise, <c>false</c>.
    /// </returns>
    internal bool TryGetSelectionSetObject(
        Cursor cursor,
        out Cursor startCursor,
        out int selectionSetId,
        out int numberOfRows)
    {
        startCursor = cursor;
        var row = _metaDb.GetValue(ref startCursor);

        if (row.TokenType is not ElementTokenType.StartObject
            || row.OperationReferenceType is not OperationReferenceType.SelectionSet)
        {
            selectionSetId = 0;
            numberOfRows = 0;
            return false;
        }

        selectionSetId = row.OperationReferenceId;
        numberOfRows = row.NumberOfRows;
        return true;
    }

    /// <summary>
    /// Gets the row offset of the value slot of <paramref name="selection"/> from the first row
    /// of an object that <paramref name="selectionSet"/> describes.
    /// </summary>
    internal static int GetValueRowOffset(SelectionSet selectionSet, Selection selection)
    {
        Debug.Assert(selection.Id > selectionSet.Id);
        return ((selection.Id - selectionSet.Id - 1) * 2) + 2;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void TrackValueReplacement(Cursor target)
    {
        if (_metaDb.GetElementTokenType(target, resolveReferences: false)
            is ElementTokenType.Reference or ElementTokenType.StartObject or ElementTokenType.StartArray)
        {
            _layoutVersion++;
            _shapeVersion++;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Cursor WriteStartArray(Cursor parent, int length = 0)
        => _metaDb.AppendStartArray(parent.Value, length, ElementFlags.None);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEndArray() => _metaDb.AppendEndArray();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteEmptyValue(Cursor parent) => _metaDb.AppendNull(parent.Value);

    private static void CheckExpectedType(ElementTokenType expected, ElementTokenType actual)
    {
        if (expected != actual)
        {
            throw new ArgumentOutOfRangeException($"Expected {expected} but found {actual}.");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _metaDb.Dispose();
    }
}

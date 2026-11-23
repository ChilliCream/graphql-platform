using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using ElementFlags = HotChocolate.Fusion.Text.Json.CompositeResultDocument.ElementFlags;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Captures the object-invariant context of a composite object element, so that
/// per-property resolution avoids repeating the StartObject resolution and
/// selection-set decode for every property.
/// </summary>
internal readonly struct CompositeObjectContext
{
    private readonly CompositeResultDocument _document;
    private readonly CompositeResultDocument.Cursor _objectStartCursor;
    private readonly SelectionSet? _selectionSet;
    private readonly int _numberOfRows;

    public CompositeObjectContext(
        CompositeResultDocument document,
        CompositeResultDocument.Cursor objectStartCursor,
        SelectionSet? selectionSet,
        int numberOfRows)
    {
        _document = document;
        _objectStartCursor = objectStartCursor;
        _selectionSet = selectionSet;
        _numberOfRows = numberOfRows;
    }

    /// <summary>
    /// Gets the selection set of the object, or <c>null</c> for a raw object.
    /// </summary>
    public SelectionSet? SelectionSet => _selectionSet;

    /// <summary>
    /// Gets a value indicating whether the properties of the object are the selections of
    /// <see cref="SelectionSet"/>.
    /// </summary>
    public bool HasSelectionProperties => _selectionSet is not null && _numberOfRows != 1;

    /// <summary>
    /// Gets the property of <paramref name="selection"/>, which must be a selection of
    /// <see cref="SelectionSet"/> of an object for which <see cref="HasSelectionProperties"/>
    /// is <c>true</c>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CompositeResultElement GetProperty(Selection selection)
    {
        Debug.Assert(HasSelectionProperties);

        var valueCursor = _objectStartCursor + CompositeResultDocument.GetValueRowOffset(_selectionSet!, selection);
        Debug.Assert(_document._metaDb.GetElementTokenType(valueCursor - 1) is ElementTokenType.PropertyName);
        Debug.Assert(_document._metaDb.Get(valueCursor - 1).OperationReferenceId == selection.Id);
        return new CompositeResultElement(_document, valueCursor);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetProperty(
        ReadOnlySpan<byte> propertyName,
        out CompositeResultElement value,
        out Selection selection)
    {
        // An object that a selection set describes resolves the property by its selection;
        // a single row (EndObject only) has no properties.
        if (_selectionSet is { } selectionSet && _numberOfRows != 1)
        {
            if (selectionSet.TryGetSelection(propertyName, out var found))
            {
                selection = found;
                var valueCursor = _objectStartCursor + CompositeResultDocument.GetValueRowOffset(selectionSet, found);
                Debug.Assert(_document._metaDb.GetElementTokenType(valueCursor - 1) is ElementTokenType.PropertyName);
                Debug.Assert(_document._metaDb.Get(valueCursor - 1).OperationReferenceId == found.Id);
                value = new CompositeResultElement(_document, valueCursor);
                return true;
            }

            value = default;
            selection = null!;
            return false;
        }

        return TryGetRawObjectProperty(propertyName, out value, out selection);
    }

    public bool TryGetVisibleProperty(
        string responseName,
        out CompositeResultElement value,
        [NotNullWhen(true)] out Selection? selection)
    {
        if (_selectionSet is not null && _selectionSet.TryGetSelection(responseName, out selection))
        {
            var cursor = _objectStartCursor + CompositeResultDocument.GetValueRowOffset(_selectionSet, selection);
            var flags = _document._metaDb.GetFlags(cursor - 1);
            if ((flags & (ElementFlags.IsInternal | ElementFlags.IsExcluded)) == ElementFlags.None)
            {
                value = new CompositeResultElement(_document, cursor);
                return true;
            }
        }

        value = default;
        selection = null;
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool TryGetRawObjectProperty(
        ReadOnlySpan<byte> propertyName,
        out CompositeResultElement value,
        out Selection selection)
    {
        // Only one row means it was EndObject.
        if (_numberOfRows == 1)
        {
            value = default;
            selection = null!;
            return false;
        }

        var endCursor = _objectStartCursor + (_numberOfRows - 1);

        if (_document.TryFindPropertyLinear(
            _objectStartCursor + 1,
            endCursor,
            propertyName,
            out value))
        {
            selection = value.AssertSelection();
            return true;
        }

        selection = null!;
        return false;
    }
}

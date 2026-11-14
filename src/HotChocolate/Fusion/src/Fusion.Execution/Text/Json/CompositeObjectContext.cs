using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;

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

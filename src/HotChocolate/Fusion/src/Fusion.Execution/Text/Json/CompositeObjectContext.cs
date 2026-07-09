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

    public bool TryGetProperty(
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

        if (_selectionSet is { } selectionSet)
        {
            if (selectionSet.TryGetSelection(propertyName, out var found))
            {
                selection = found;
                var propertyIndex = found.Id - selectionSet.Id - 1;
                var propertyRowIndex = (propertyIndex * 2) + 1;
                var propertyCursor = _objectStartCursor + propertyRowIndex;
                value = new CompositeResultElement(_document, propertyCursor + 1);
                return true;
            }

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

using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Enumerates the elements of a source result array in document order and exposes
/// the current element as a <see cref="SourceResultElementSnapshot"/>.
/// The enumerator is only valid while the parent <see cref="SourceResultDocument"/> is alive.
/// </summary>
internal ref struct SourceElementSnapshotEnumerator
{
    private readonly SourceResultDocument _parent;
    private readonly SourceResultDocument.Cursor _endCursor;
    private SourceResultDocument.Cursor _nextCursor;
    private SourceResultDocument.Cursor _cursor;
    private SourceResultDocument.DbRow _row;

    internal SourceElementSnapshotEnumerator(
        SourceResultDocument parent,
        SourceResultDocument.Cursor arrayCursor,
        SourceResultDocument.DbRow arrayRow)
    {
        Debug.Assert(arrayRow.TokenType is JsonTokenType.StartArray);

        _parent = parent;
        _endCursor = arrayCursor + (arrayRow.NumberOfRows - 1);
        _nextCursor = arrayCursor + 1;
        _cursor = default;
        _row = default;
    }

    /// <summary>
    /// Gets the current element.
    /// </summary>
    public readonly SourceResultElementSnapshot Current => new(_parent, _cursor, _row);

    /// <summary>
    /// Advances to the next element of the array.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the enumerator advanced to an element;
    /// <c>false</c>, if there are no more elements.
    /// </returns>
    public bool MoveNext()
    {
        var cursor = _nextCursor;

        if (cursor < _endCursor)
        {
            var row = _parent.GetDbRow(cursor);
            _cursor = cursor;
            _row = row;
            _nextCursor = cursor + (row.IsSimpleValue ? 1 : row.NumberOfRows);
            return true;
        }

        return false;
    }
}

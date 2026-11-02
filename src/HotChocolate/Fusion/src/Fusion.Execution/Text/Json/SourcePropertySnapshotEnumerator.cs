using System.Diagnostics;
using System.Text.Json;

namespace HotChocolate.Fusion.Text.Json;

/// <summary>
/// Enumerates the properties of a source result object in document order and exposes
/// the value of the current property as a <see cref="SourceResultElementSnapshot"/>.
/// The enumerator is only valid while the parent <see cref="SourceResultDocument"/> is alive.
/// </summary>
internal ref struct SourcePropertySnapshotEnumerator
{
    private readonly SourceResultDocument _parent;
    private readonly SourceResultDocument.Cursor _endCursor;
    private SourceResultDocument.Cursor _nextCursor;
    private SourceResultDocument.Cursor _cursor;
    private SourceResultDocument.DbRow _row;

    internal SourcePropertySnapshotEnumerator(
        SourceResultDocument parent,
        SourceResultDocument.Cursor objectCursor,
        SourceResultDocument.DbRow objectRow)
    {
        Debug.Assert(objectRow.TokenType is JsonTokenType.StartObject);

        _parent = parent;
        _endCursor = objectCursor + (objectRow.NumberOfRows - 1);
        _nextCursor = objectCursor + 2;
        _cursor = default;
        _row = default;
    }

    /// <summary>
    /// Gets the raw UTF-8 name of the current property. JSON escape sequences are not decoded.
    /// </summary>
    public readonly ReadOnlySpan<byte> Name => _parent.GetPropertyNameRaw(_cursor);

    /// <summary>
    /// Gets the value of the current property.
    /// </summary>
    public readonly SourceResultElementSnapshot Value => new(_parent, _cursor, _row);

    /// <summary>
    /// Advances to the next property of the object.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if the enumerator advanced to a property;
    /// <c>false</c>, if there are no more properties.
    /// </returns>
    public bool MoveNext()
    {
        var cursor = _nextCursor;

        if (cursor < _endCursor)
        {
            var row = _parent.GetDbRow(cursor);
            _cursor = cursor;
            _row = row;

            // The next value follows the rows of this value and the name row of the next property.
            _nextCursor = cursor + ((row.IsSimpleValue ? 1 : row.NumberOfRows) + 1);
            return true;
        }

        return false;
    }
}

namespace HotChocolate.Fusion.Text.Json;

internal struct SourceResultValueEnumerator
{
    private readonly SourceResultDocument _parent;
    private readonly SourceResultDocument.Cursor _endCursor;
    private readonly int _propertyNameOffset;
    private SourceResultDocument.Cursor _nextCursor;
    private SourceResultDocument.Cursor _currentCursor;
    private SourceResultDocument.DbRow _currentRow;

    internal SourceResultValueEnumerator(SourceResultElementSnapshot target, int propertyNameOffset)
    {
        _parent = target._parent;
        _endCursor = target._cursor + (target._row.NumberOfRows - 1);
        _propertyNameOffset = propertyNameOffset;
        _nextCursor = target._cursor + (1 + propertyNameOffset);
        _currentCursor = default;
        _currentRow = default;
    }

    public readonly SourceResultElementSnapshot Current => new(_parent, _currentCursor, _currentRow);

    internal readonly SourceResultProperty CurrentProperty => new(new(_parent, _currentCursor));

    public bool MoveNext()
    {
        if (_nextCursor >= _endCursor)
        {
            return false;
        }

        _currentCursor = _nextCursor;
        _currentRow = _parent.GetDbRow(_currentCursor);
        _nextCursor = _currentCursor + (_currentRow.NumberOfRows + _propertyNameOffset);
        return true;
    }
}

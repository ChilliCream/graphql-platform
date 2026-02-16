using System.Collections;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Resolvers;

/// <summary>
/// An SelectionSet enumerator.
/// </summary>
public struct SelectionEnumerator : IEnumerable<Selection>, IEnumerator<Selection>
{
    private readonly SelectionSet _selectionSet;
    private readonly ulong _includeFlags;
    private int _position = -1;

    /// <summary>
    /// Initializes a new instance of <see cref="SelectionEnumerator"/>
    /// </summary>
    /// <param name="selectionSet">
    /// The selection set to enumerate on.
    /// </param>
    /// <param name="includeFlags">
    /// The include flags representing the selections that shall be included.
    /// </param>
    public SelectionEnumerator(SelectionSet selectionSet, ulong includeFlags)
    {
        _selectionSet = selectionSet;
        _includeFlags = includeFlags;
        Current = null!;
    }

    /// <summary>
    /// The currently selected selection.
    /// </summary>
    public Selection Current { get; private set; }

    object? IEnumerator.Current => Current;

    /// <summary>
    /// Moves to the next visible selection.
    /// </summary>
    /// <returns>
    /// <c>true</c> if there is another visible selection.
    /// </returns>
    public bool MoveNext()
    {
        if (_selectionSet is null)
        {
            return false;
        }

        var length = _selectionSet.Selections.Length;

        while (_position < length)
        {
            _position++;

            if (_position >= length)
            {
                break;
            }

            var selection = _selectionSet.Selections[_position];
            if (selection.IsIncluded(_includeFlags))
            {
                Current = selection;
                return true;
            }
        }

        Current = null!;
        return false;
    }

    public void Reset()
    {
        _position = -1;
    }

    public SelectionEnumerator GetEnumerator() => this;

    IEnumerator<Selection> IEnumerable<Selection>.GetEnumerator() => this;

    IEnumerator IEnumerable.GetEnumerator() => this;

    public void Dispose() { }
}

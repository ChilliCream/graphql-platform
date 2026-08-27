using System.Collections;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Resolvers;

/// <summary>
/// An SelectionSet enumerator.
/// </summary>
public struct SelectionEnumerator : IEnumerable<Selection>, IEnumerator<Selection>
{
    private readonly SelectionSet _selectionSet;
    private readonly ulong _includeFlags;
    private readonly ulong[]? _wideIncludeFlags;
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
        if (selectionSet?.DeclaringOperation.HasWideIncludeFlags == true)
        {
            throw new InvalidOperationException(
                "The operation has more than 64 include conditions; this enumerator "
                + "requires the wide include flags. Use IResolverContext.GetSelections.");
        }

        _selectionSet = selectionSet!;
        _includeFlags = includeFlags;
        Current = null!;
    }

    internal SelectionEnumerator(SelectionSet selectionSet, ConditionFlags includeFlags)
    {
        _selectionSet = selectionSet;
        _includeFlags = includeFlags.Word0;
        _wideIncludeFlags = includeFlags.Overflow;
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

        if (_wideIncludeFlags is not null)
        {
            return MoveNextWide();
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
            if (selection.IsIncludedUnchecked(_includeFlags))
            {
                Current = selection;
                return true;
            }
        }

        Current = null!;
        return false;
    }

    private bool MoveNextWide()
    {
        var length = _selectionSet.Selections.Length;

        while (_position < length)
        {
            _position++;

            if (_position >= length)
            {
                break;
            }

            var selection = _selectionSet.Selections[_position];
            if (selection.IsIncludedWide(_includeFlags, _wideIncludeFlags))
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

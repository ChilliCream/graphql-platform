namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// A selection set with 8 or more direct selections. Uses a dictionary for O(1) child lookup.
/// Handles rare wide selection sets.
/// </summary>
internal sealed class LargeResultSelectionSet : ResultSelectionSet
{
    private readonly ResultSelection[] _selections;
    private readonly Dictionary<string, ResultSelectionSet?> _childLookup;

    internal LargeResultSelectionSet(
        ResultSelection[] selections,
        ResultFragment[] fragments,
        string[] allResponseNames)
        : base(fragments, allResponseNames)
    {
        _selections = selections;
        _childLookup = new Dictionary<string, ResultSelectionSet?>(selections.Length, StringComparer.Ordinal);

        for (var i = 0; i < selections.Length; i++)
        {
            _childLookup[selections[i].ResponseName] = selections[i].Child;
        }
    }

    protected override ReadOnlySpan<ResultSelection> DirectSelections => _selections;

    protected override bool TryGetDirectChild(string responseName, out ResultSelectionSet? child)
        => _childLookup.TryGetValue(responseName, out child);
}

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// A selection set with fewer than 8 direct selections. Uses linear scan for lookups.
/// Cache-friendly and avoids hash overhead. Covers the vast majority of selection sets.
/// </summary>
internal sealed class SmallResultSelectionSet(
    ResultSelection[] selections,
    ResultFragment[] fragments,
    string[] allResponseNames)
    : ResultSelectionSet(fragments, allResponseNames)
{
    protected override ReadOnlySpan<ResultSelection> DirectSelections => selections;

    protected override bool TryGetDirectChild(string responseName, out ResultSelectionSet? child)
    {
        for (var i = 0; i < selections.Length; i++)
        {
            if (string.Equals(selections[i].ResponseName, responseName, StringComparison.Ordinal))
            {
                child = selections[i].Child;
                return true;
            }
        }

        child = null;
        return false;
    }
}

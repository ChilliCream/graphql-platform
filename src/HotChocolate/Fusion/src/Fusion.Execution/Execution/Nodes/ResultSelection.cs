namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Represents a direct field selection with its response name and optional child selection set.
/// </summary>
internal readonly struct ResultSelection(string responseName, ResultSelectionSet? child)
{
    public string ResponseName { get; } = responseName;
    public ResultSelectionSet? Child { get; } = child;
}

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Provides information about the executable nodes that are associated a selection-set and
/// the state that is created by doing so.
/// </summary>
internal readonly struct SelectionSetInfo
{
    public SelectionSetInfo(int nodeCount, IReadOnlyList<string> exportKeys)
    {
        NodeCount = nodeCount;
        ExportKeys = exportKeys;
    }

    /// <summary>
    /// Gets the number of nodes that need to be executed for the specified selection-set.
    /// </summary>
    public int NodeCount { get; }

    /// <summary>
    /// Gets the state that the execution of the nodes produce for this selection-set.
    /// </summary>
    public IReadOnlyList<string> ExportKeys { get; }
}

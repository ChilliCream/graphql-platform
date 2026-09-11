namespace HotChocolate.CostAnalysis;

/// <summary>
/// The extracted condition tree of one selection-set boundary: a
/// hash-consed arena of nodes, one per distinct cumulative condition
/// reachable in that boundary.
/// </summary>
internal sealed class ConditionTree
{
    private const int IndexThreshold = 8;

    public ConditionTree(int rootNodeId, IReadOnlyList<ConditionTreeNode> nodes)
    {
        RootNodeId = rootNodeId;
        Nodes = nodes;

        var responseNames = new List<string>();
        Dictionary<string, int>? responseNameIndex = null;
        var groups = new List<FieldGroup>();
        var hasUniqueResponseNames = true;

        foreach (var node in nodes)
        {
            foreach (var group in node.FieldGroups)
            {
                var responseNameId = FindResponseName(
                    responseNames,
                    responseNameIndex,
                    group.ResponseName);

                if (responseNameId < 0)
                {
                    responseNameId = responseNames.Count;
                    responseNames.Add(group.ResponseName);

                    if (responseNameIndex is not null)
                    {
                        responseNameIndex.Add(group.ResponseName, responseNameId);
                    }
                    else if (responseNames.Count > IndexThreshold)
                    {
                        responseNameIndex = BuildResponseNameIndex(responseNames);
                    }
                }
                else
                {
                    hasUniqueResponseNames = false;
                }

                group.InitializeLayout(groups.Count, responseNameId);
                groups.Add(group);
            }
        }

        Groups = [.. groups];
        ResponseNameCount = responseNames.Count;
        HasUniqueResponseNames = hasUniqueResponseNames;
    }

    /// <summary>
    /// Gets the id of the boundary's root node.
    /// </summary>
    public int RootNodeId { get; }

    /// <summary>
    /// Gets every node in this tree, indexed by its id.
    /// </summary>
    public IReadOnlyList<ConditionTreeNode> Nodes { get; }

    /// <summary>
    /// Gets whether every field group in this tree has a distinct response name.
    /// </summary>
    public bool HasUniqueResponseNames { get; }

    internal FieldGroup[] Groups { get; }

    internal int ResponseNameCount { get; }

    /// <summary>
    /// Gets the boundary's root node.
    /// </summary>
    public ConditionTreeNode Root => Nodes[RootNodeId];

    private static int FindResponseName(
        IReadOnlyList<string> responseNames,
        Dictionary<string, int>? index,
        string responseName)
    {
        if (index is not null)
        {
            return index.TryGetValue(responseName, out var responseNameId)
                ? responseNameId
                : -1;
        }

        for (var i = 0; i < responseNames.Count; i++)
        {
            if (responseNames[i] == responseName)
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildResponseNameIndex(
        IReadOnlyList<string> responseNames)
    {
        var index = new Dictionary<string, int>(
            responseNames.Count,
            StringComparer.Ordinal);

        for (var i = 0; i < responseNames.Count; i++)
        {
            index.Add(responseNames[i], i);
        }

        return index;
    }
}

using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Merges the field groups of every condition-tree node visited for one
/// exact case, by response name, in first-occurrence order.
/// </summary>
internal static class FieldGroupMerger
{
    private const int IndexThreshold = 8;

    /// <summary>
    /// Merges the field groups of <paramref name="visited"/> nodes by
    /// response name, in first-occurrence order across the visited nodes.
    /// </summary>
    public static List<(string ResponseName, List<FieldNode> Fields)> Merge(
        ConditionTree tree,
        IReadOnlyList<int> visited)
    {
        var order = new List<(string ResponseName, List<FieldNode> Fields)>();
        Dictionary<string, int>? indexByResponseName = null;

        foreach (var nodeId in visited)
        {
            foreach (var group in tree.Nodes[nodeId].FieldGroups)
            {
                var index = FindGroup(order, indexByResponseName, group.ResponseName);

                if (index < 0)
                {
                    index = order.Count;
                    order.Add((group.ResponseName, []));

                    if (indexByResponseName is not null)
                    {
                        indexByResponseName.Add(group.ResponseName, index);
                    }
                    else if (order.Count > IndexThreshold)
                    {
                        indexByResponseName = BuildIndex(order);
                    }
                }

                order[index].Fields.AddRange(group.Fields);
            }
        }

        return order;
    }

    private static int FindGroup(
        List<(string ResponseName, List<FieldNode> Fields)> groups,
        Dictionary<string, int>? index,
        string responseName)
    {
        if (index is not null)
        {
            return index.TryGetValue(responseName, out var groupIndex) ? groupIndex : -1;
        }

        for (var i = 0; i < groups.Count; i++)
        {
            if (groups[i].ResponseName == responseName)
            {
                return i;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildIndex(
        List<(string ResponseName, List<FieldNode> Fields)> groups)
    {
        var index = new Dictionary<string, int>(groups.Count, StringComparer.Ordinal);

        for (var i = 0; i < groups.Count; i++)
        {
            index.Add(groups[i].ResponseName, i);
        }

        return index;
    }

    /// <summary>
    /// Concatenates the selection sets of every field in
    /// <paramref name="fields"/> into one merged selection list, the shape
    /// of the spec's CollectSubfields.
    /// </summary>
    public static IReadOnlyList<ISelectionNode> MergedSelections(IReadOnlyList<FieldNode> fields)
    {
        if (fields.Count == 1)
        {
            return fields[0].SelectionSet?.Selections ?? [];
        }

        List<ISelectionNode>? merged = null;

        foreach (var field in fields)
        {
            if (field.SelectionSet is { } selectionSet)
            {
                (merged ??= []).AddRange(selectionSet.Selections);
            }
        }

        return merged ?? [];
    }
}

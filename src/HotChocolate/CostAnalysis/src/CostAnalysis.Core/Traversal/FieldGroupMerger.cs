using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Merges the field groups of every condition-tree node visited for one
/// exact case, by response name, in first-occurrence order.
/// </summary>
internal static class FieldGroupMerger
{
    /// <summary>
    /// Merges the field groups of <paramref name="visited"/> nodes by
    /// response name, in first-occurrence order across the visited nodes.
    /// </summary>
    public static List<(string ResponseName, List<FieldNode> Fields)> Merge(ConditionTree tree, List<int> visited)
    {
        var order = new List<(string ResponseName, List<FieldNode> Fields)>();
        var indexByResponseName = new Dictionary<string, int>();

        foreach (var nodeId in visited)
        {
            foreach (var group in tree.Nodes[nodeId].FieldGroups)
            {
                if (!indexByResponseName.TryGetValue(group.ResponseName, out var index))
                {
                    index = order.Count;
                    indexByResponseName.Add(group.ResponseName, index);
                    order.Add((group.ResponseName, []));
                }

                order[index].Fields.AddRange(group.Fields);
            }
        }

        return order;
    }

    /// <summary>
    /// Concatenates the selection sets of every field in
    /// <paramref name="fields"/> into one merged selection list, the shape
    /// of the spec's CollectSubfields.
    /// </summary>
    public static IReadOnlyList<ISelectionNode> MergedSelections(List<FieldNode> fields)
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

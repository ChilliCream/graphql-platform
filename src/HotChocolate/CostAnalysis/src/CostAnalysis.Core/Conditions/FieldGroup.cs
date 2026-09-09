using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Every selection under one condition-tree node that shares a response
/// name, in first-occurrence order.
/// </summary>
internal sealed class FieldGroup(string responseName)
{
    private readonly List<FieldNode> _fields = [];

    /// <summary>
    /// Gets the response name this group was collected under.
    /// </summary>
    public string ResponseName { get; } = responseName;

    /// <summary>
    /// Gets the group's fields, in first-occurrence order.
    /// </summary>
    public IReadOnlyList<FieldNode> Fields => _fields;

    internal int GroupId { get; private set; } = -1;

    internal int ResponseNameId { get; private set; } = -1;

    internal void Add(FieldNode field) => _fields.Add(field);

    internal void InitializeLayout(int groupId, int responseNameId)
    {
        GroupId = groupId;
        ResponseNameId = responseNameId;
    }

    /// <summary>
    /// Concatenates the selection sets of every field in this group into one
    /// merged selection list, the shape of the spec's CollectSubfields.
    /// </summary>
    public IReadOnlyList<ISelectionNode> MergedSelectionSet()
    {
        if (_fields.Count == 1)
        {
            return _fields[0].SelectionSet?.Selections ?? [];
        }

        List<ISelectionNode>? merged = null;

        foreach (var field in _fields)
        {
            if (field.SelectionSet is { } selectionSet)
            {
                (merged ??= []).AddRange(selectionSet.Selections);
            }
        }

        return merged ?? [];
    }
}

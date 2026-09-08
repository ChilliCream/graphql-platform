using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One node of a condition tree: a cumulative <see cref="Condition"/>, the
/// response-name field groups collected under it, and the edges to nodes
/// reached by narrowing it further.
/// </summary>
internal sealed class ConditionTreeNode(Condition condition)
{
    // Adaptive response-name lookup: a linear scan over the small groups
    // list is cheapest for the common few-fields case; past 8 groups an
    // index is built once and kept in sync with further additions.
    private const int IndexThreshold = 8;

    private readonly List<FieldGroup> _fieldGroups = [];
    private readonly List<Branch> _branches = [];
    private Dictionary<string, int>? _fieldGroupIndex;

    /// <summary>
    /// Gets the cumulative condition this node represents.
    /// </summary>
    public Condition Condition { get; } = condition;

    /// <summary>
    /// Gets the node's field groups, ordered by first occurrence.
    /// </summary>
    public IReadOnlyList<FieldGroup> FieldGroups => _fieldGroups;

    /// <summary>
    /// Gets the edges leading from this node to nodes reached by narrowing
    /// its condition further.
    /// </summary>
    public IReadOnlyList<Branch> Branches => _branches;

    /// <summary>
    /// Adds <paramref name="field"/> to this node's group for its response
    /// name, creating the group on first occurrence.
    /// </summary>
    internal void AddField(FieldNode field)
    {
        var responseName = (field.Alias ?? field.Name).Value;
        FindOrCreateGroup(responseName).Add(field);
    }

    /// <summary>
    /// Adds an edge to <paramref name="targetNodeId"/> labeled
    /// <paramref name="branchCondition"/>, unless an identical edge already
    /// exists.
    /// </summary>
    internal void AddBranch(BranchCondition branchCondition, int targetNodeId)
    {
        foreach (var branch in _branches)
        {
            if (branch.TargetNodeId == targetNodeId && branch.Condition.Equals(branchCondition))
            {
                return;
            }
        }

        _branches.Add(new Branch(branchCondition, targetNodeId));
    }

    private FieldGroup FindOrCreateGroup(string responseName)
    {
        if (_fieldGroupIndex is { } index)
        {
            if (index.TryGetValue(responseName, out var existingIndex))
            {
                return _fieldGroups[existingIndex];
            }

            var group = new FieldGroup(responseName);
            index.Add(responseName, _fieldGroups.Count);
            _fieldGroups.Add(group);
            return group;
        }

        foreach (var candidate in _fieldGroups)
        {
            if (candidate.ResponseName == responseName)
            {
                return candidate;
            }
        }

        var created = new FieldGroup(responseName);
        _fieldGroups.Add(created);

        if (_fieldGroups.Count > IndexThreshold)
        {
            BuildFieldGroupIndex();
        }

        return created;
    }

    private void BuildFieldGroupIndex()
    {
        var index = new Dictionary<string, int>(_fieldGroups.Count);

        for (var i = 0; i < _fieldGroups.Count; i++)
        {
            index[_fieldGroups[i].ResponseName] = i;
        }

        _fieldGroupIndex = index;
    }
}

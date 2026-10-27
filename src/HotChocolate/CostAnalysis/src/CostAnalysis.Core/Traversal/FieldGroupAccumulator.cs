using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

internal ref struct FieldGroupAccumulator
{
    public const int MaxStackScratchLength = 512;

    private readonly ConditionTree _tree;
    private readonly Span<int> _heads;
    private readonly Span<int> _tails;
    private readonly Span<int> _order;
    private readonly Span<int> _groupIds;
    private readonly Span<int> _next;
    private int _count;

    public FieldGroupAccumulator(ConditionTree tree, Span<int> scratch)
    {
        _tree = tree;
        var responseNameCount = tree.ResponseNameCount;
        var groupCount = tree.Groups.Length;
        _heads = scratch[..responseNameCount];
        _tails = scratch.Slice(responseNameCount, responseNameCount);
        _order = scratch.Slice(responseNameCount * 2, responseNameCount);
        _groupIds = scratch.Slice(responseNameCount * 3, groupCount);
        _next = scratch.Slice(responseNameCount * 3 + groupCount, groupCount);
        _count = 0;
    }

    public int Count => _count;

    public static int GetRequiredScratchLength(ConditionTree tree)
        => (tree.ResponseNameCount * 3) + (tree.Groups.Length * 2);

    public void Build(IReadOnlyList<int> visited)
    {
        _heads.Fill(-1);
        _count = 0;
        var entryIndex = 0;

        foreach (var nodeId in visited)
        {
            foreach (var group in _tree.Nodes[nodeId].FieldGroups)
            {
                var responseNameId = group.ResponseNameId;
                _groupIds[entryIndex] = group.GroupId;
                _next[entryIndex] = -1;

                if (_heads[responseNameId] < 0)
                {
                    _heads[responseNameId] = entryIndex;
                    _order[_count++] = responseNameId;
                }
                else
                {
                    _next[_tails[responseNameId]] = entryIndex;
                }

                _tails[responseNameId] = entryIndex;
                entryIndex++;
            }
        }
    }

    public int GetResponseNameId(int index) => _order[index];

    public int GetFirstEntry(int responseNameId) => _heads[responseNameId];

    public int GetNextEntry(int entry) => _next[entry];

    public FieldGroup GetGroup(int entry) => _tree.Groups[_groupIds[entry]];

    public bool HasChildSelections(int responseNameId)
    {
        for (var entry = GetFirstEntry(responseNameId);
            entry >= 0;
            entry = GetNextEntry(entry))
        {
            foreach (var field in GetGroup(entry).Fields)
            {
                if (field.SelectionSet is { Selections.Count: > 0 })
                {
                    return true;
                }
            }
        }

        return false;
    }

    public FieldNode[] MaterializeFields(int responseNameId)
    {
        var count = 0;

        for (var entry = GetFirstEntry(responseNameId);
            entry >= 0;
            entry = GetNextEntry(entry))
        {
            count += GetGroup(entry).Fields.Count;
        }

        var fields = new FieldNode[count];
        var index = 0;

        for (var entry = GetFirstEntry(responseNameId);
            entry >= 0;
            entry = GetNextEntry(entry))
        {
            foreach (var field in GetGroup(entry).Fields)
            {
                fields[index++] = field;
            }
        }

        return fields;
    }
}

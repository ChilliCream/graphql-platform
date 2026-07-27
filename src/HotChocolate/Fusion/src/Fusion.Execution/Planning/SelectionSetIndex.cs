using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndex : ISelectionSetIndex
{
    private readonly ImmutableDictionary<SelectionSetNode, uint> _selectionSets;
    private readonly ImmutableDictionary<uint, SelectionSetNode> _selectionSetById;
    private readonly ImmutableDictionary<uint, uint> _clonedToOriginalMap;
    private readonly ImmutableDictionary<uint, ConcreteBranchScope> _concreteBranchScopes;
    private readonly uint _nextId;

    internal SelectionSetIndex(
        ImmutableDictionary<SelectionSetNode, uint> selectionSets,
        ImmutableDictionary<uint, SelectionSetNode> selectionSetById,
        ImmutableDictionary<uint, uint> clonedToOriginalMap,
        ImmutableDictionary<uint, ConcreteBranchScope> concreteBranchScopes,
        uint nextId)
    {
        _selectionSets = selectionSets;
        _selectionSetById = selectionSetById;
        _clonedToOriginalMap = clonedToOriginalMap;
        _concreteBranchScopes = concreteBranchScopes;
        _nextId = nextId;
    }

    public uint GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetSelectionSet(uint id, out SelectionSetNode selectionSet)
        => _selectionSetById.TryGetValue(id, out selectionSet!);

    public bool TryGetOriginalIdFromCloned(uint clonedId, out uint originalId)
        => _clonedToOriginalMap.TryGetValue(clonedId, out originalId);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public SelectionSetIndexBuilder ToBuilder()
        => new(
            _selectionSets,
            _selectionSetById,
            _clonedToOriginalMap,
            _concreteBranchScopes,
            _nextId);
}

internal readonly record struct ConcreteBranchScope(
    uint ParentSelectionSetId,
    string TypeCondition);

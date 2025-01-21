using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed class SelectionSetIndex : ISelectionSetIndex
{
    private readonly ImmutableDictionary<SelectionSetNode, int> _selectionSets;

    internal SelectionSetIndex(ImmutableDictionary<SelectionSetNode, int> selectionSets)
    {
        _selectionSets = selectionSets;
    }

    public int GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetId(SelectionSetNode selectionSet, out int id)
        => _selectionSets.TryGetValue(selectionSet, out id);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public SelectionSetIndexBuilder ToBuilder()
        => new(_selectionSets);
}

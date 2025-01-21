using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed class SelectionSetIndexBuilder : ISelectionSetIndex
{
    private ImmutableDictionary<SelectionSetNode, int> _selectionSets;

    internal SelectionSetIndexBuilder(ImmutableDictionary<SelectionSetNode, int> selectionSets)
    {
        _selectionSets = selectionSets;
    }

    public int GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetId(SelectionSetNode selectionSet, out int id)
        => _selectionSets.TryGetValue(selectionSet, out id);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public void Register(SelectionSet original, SelectionSetNode branch)
        => _selectionSets = _selectionSets.SetItem(branch, original.Id);

    public void Register(SelectionSetNode original, SelectionSetNode branch)
    {
        var id = _selectionSets[original];
        _selectionSets = _selectionSets.SetItem(branch, id);
    }

    public ISelectionSetIndex Build()
        => new SelectionSetIndex(_selectionSets);

    public SelectionSetIndexBuilder ToBuilder()
        => this;
}

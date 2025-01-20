using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public sealed class SelectionSetIndex(ImmutableDictionary<SelectionSetNode, int> selectionSets)
{
    private ImmutableDictionary<SelectionSetNode, int> _selectionSets = selectionSets;

    public int GetSelectionSetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public void RegisterSelectionSet(SelectionSetNode original, SelectionSetNode branch)
    {
        var id = _selectionSets[original];
        _selectionSets = _selectionSets.SetItem(branch, id);
    }

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public SelectionSetIndex Branch()
        => new(_selectionSets);
}

using System.Collections.Immutable;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndexBuilder : ISelectionSetIndex
{
    private ImmutableDictionary<SelectionSetNode, uint> _selectionSets;
    private uint _nextId;

    internal SelectionSetIndexBuilder(ImmutableDictionary<SelectionSetNode, uint> selectionSets, uint nextId)
    {
        _selectionSets = selectionSets;
        _nextId = nextId;
    }

    public uint GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetId(SelectionSetNode selectionSet, out uint id)
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

    public void Register(SelectionSetNode original)
        => _selectionSets = _selectionSets.SetItem(original, _nextId++);

    public ISelectionSetIndex Build()
        => new SelectionSetIndex(_selectionSets, _nextId);

    public SelectionSetIndexBuilder ToBuilder()
        => this;
}

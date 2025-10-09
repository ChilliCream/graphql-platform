using System.Collections.Immutable;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndexBuilder : ISelectionSetIndex, ISelectionSetMergeObserver
{
    // private List<SelectionSetNode>? _temp;
    private ImmutableDictionary<SelectionSetNode, uint> _selectionSets;
    private ImmutableDictionary<uint, uint> _clonedToOriginalMap;
    private uint _nextId;

    internal SelectionSetIndexBuilder(
        ImmutableDictionary<SelectionSetNode, uint> selectionSets,
        ImmutableDictionary<uint, uint> clonedToOriginalMap,
        uint nextId)
    {
        _selectionSets = selectionSets;
        _clonedToOriginalMap = clonedToOriginalMap;
        _nextId = nextId;
    }

    public uint GetId(SelectionSetNode selectionSet)
        => _selectionSets[selectionSet];

    public bool TryGetId(SelectionSetNode selectionSet, out uint id)
        => _selectionSets.TryGetValue(selectionSet, out id);

    public bool TryGetOriginalIdFromCloned(uint clonedId, out uint originalId)
        => _clonedToOriginalMap.TryGetValue(clonedId, out originalId);

    public bool IsRegistered(SelectionSetNode selectionSet)
        => _selectionSets.ContainsKey(selectionSet);

    public void Register(uint id, SelectionSetNode branch)
        => _selectionSets = _selectionSets.SetItem(branch, id);

    public void Register(SelectionSet original, SelectionSetNode branch)
        => _selectionSets = _selectionSets.SetItem(branch, original.Id);

    public void Register(SelectionSetNode original, SelectionSetNode branch)
    {
        var id = _selectionSets[original];
        _selectionSets = _selectionSets.SetItem(branch, id);
    }

    public void Register(SelectionSetNode original)
        => _selectionSets = _selectionSets.SetItem(original, _nextId++);

    public void RegisterCloned(SelectionSetNode original, SelectionSetNode cloned)
    {
        Register(cloned);

        var clonedId = _selectionSets[cloned];
        var originalId = _selectionSets[original];

        _clonedToOriginalMap = _clonedToOriginalMap.SetItem(clonedId, originalId);
    }

    // public void OnMerge(IEnumerable<SelectionSetNode> selectionSets)
    // {
    //     var temp = RentSelectionSetList();
    //     uint? key = null;
    //
    //     foreach (var s in selectionSets)
    //     {
    //         if (!key.HasValue && _selectionSets.TryGetValue(s, out var id))
    //         {
    //             key = id;
    //         }
    //
    //         temp.Add(s);
    //     }
    //
    //     key ??= _nextId;
    //
    //     foreach (var s in temp)
    //     {
    //         _selectionSets = _selectionSets.SetItem(s, key.Value);
    //     }
    //
    //     ReturnSelectionSetList(temp);
    // }

    public void OnMerge(SelectionSetNode newSelectionSetNode, params IEnumerable<SelectionSetNode> oldSelectionSetNodes)
    {
        // TODO: This is just temporary
        foreach (var oldSelectionSetNode in oldSelectionSetNodes)
        {
            if (!_selectionSets.TryGetValue(oldSelectionSetNode, out var oldId))
            {
                if (!_selectionSets.TryGetValue(newSelectionSetNode, out var newId))
                {
                    newId = _nextId++;
                    _selectionSets = _selectionSets.SetItem(oldSelectionSetNode, newId);
                    _selectionSets = _selectionSets.SetItem(newSelectionSetNode, newId);
                }
                else
                {
                    _selectionSets = _selectionSets.SetItem(oldSelectionSetNode, newId);
                }
            }
            else
            {
                _selectionSets = _selectionSets.SetItem(newSelectionSetNode, oldId);
            }
        }
    }

    public ISelectionSetIndex Build()
        => new SelectionSetIndex(_selectionSets, _clonedToOriginalMap, _nextId);

    public SelectionSetIndexBuilder ToBuilder()
        => this;
}

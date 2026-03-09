using System.Collections.Immutable;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed class SelectionSetIndexBuilder : ISelectionSetIndex, ISelectionSetMergeObserver
{
    private List<SelectionSetNode>? _temp;
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

    public void OnMerge(FieldNode field1, FieldNode field2)
    {
        if (field1.SelectionSet is null)
        {
            if (field2.SelectionSet is not null
                && !_selectionSets.TryGetKey(field2.SelectionSet, out _))
            {
                Register(field2.SelectionSet);
            }
            return;
        }

        if (field2.SelectionSet is null)
        {
            if (!_selectionSets.TryGetKey(field1.SelectionSet, out _))
            {
                Register(field1.SelectionSet);
            }

            return;
        }

        if (!_selectionSets.TryGetValue(field1.SelectionSet, out var id))
        {
            if (!_selectionSets.TryGetValue(field2.SelectionSet, out id))
            {
                id = _nextId++;
                _selectionSets = _selectionSets.SetItem(field1.SelectionSet, id);
                _selectionSets = _selectionSets.SetItem(field2.SelectionSet, id);
            }
            else
            {
                _selectionSets = _selectionSets.SetItem(field1.SelectionSet, id);
            }
        }
        else
        {
            _selectionSets = _selectionSets.SetItem(field2.SelectionSet, id);
        }
    }

    public void OnMerge(IEnumerable<FieldNode> fields)
    {
        var temp = RentSelectionSetList();
        uint? key = null;

        foreach (var field in fields)
        {
            if (field.SelectionSet is null)
            {
                ReturnSelectionSetList(temp);
                return;
            }

            if (!key.HasValue && _selectionSets.TryGetValue(field.SelectionSet, out var id))
            {
                key = id;
            }

            temp.Add(field.SelectionSet);
        }

        key ??= _nextId;

        foreach (var selectionSet in temp)
        {
            _selectionSets = _selectionSets.SetItem(selectionSet, key.Value);
        }

        ReturnSelectionSetList(temp);
    }

    public void OnMerge(InlineFragmentNode inlineFragment1, InlineFragmentNode inlineFragment2)
    {
        var s1 = inlineFragment1.SelectionSet;
        var s2 = inlineFragment2.SelectionSet;

        if (!_selectionSets.TryGetValue(s1, out var id))
        {
            if (!_selectionSets.TryGetValue(s2, out id))
            {
                id = _nextId++;
                _selectionSets = _selectionSets.SetItem(s1, id);
                _selectionSets = _selectionSets.SetItem(s2, id);
            }
            else
            {
                _selectionSets = _selectionSets.SetItem(s1, id);
            }
        }
        else
        {
            _selectionSets = _selectionSets.SetItem(s2, id);
        }
    }

    public void OnMerge(SelectionSetNode selectionSet1, SelectionSetNode selectionSet2)
    {
        if (!_selectionSets.TryGetValue(selectionSet1, out var id))
        {
            if (!_selectionSets.TryGetValue(selectionSet2, out id))
            {
                id = _nextId++;
                _selectionSets = _selectionSets.SetItem(selectionSet1, id);
                _selectionSets = _selectionSets.SetItem(selectionSet2, id);
            }
            else
            {
                _selectionSets = _selectionSets.SetItem(selectionSet1, id);
            }
        }
        else
        {
            _selectionSets = _selectionSets.SetItem(selectionSet2, id);
        }
    }

    public void OnMerge(IEnumerable<SelectionSetNode> selectionSets)
    {
        var temp = RentSelectionSetList();
        uint? key = null;

        foreach (var s in selectionSets)
        {
            if (!key.HasValue && _selectionSets.TryGetValue(s, out var id))
            {
                key = id;
            }

            temp.Add(s);
        }

        key ??= _nextId;

        foreach (var s in temp)
        {
            _selectionSets = _selectionSets.SetItem(s, key.Value);
        }

        ReturnSelectionSetList(temp);
    }

    private List<SelectionSetNode> RentSelectionSetList()
        => Interlocked.Exchange(ref _temp, null) ?? [];

    private void ReturnSelectionSetList(List<SelectionSetNode> selectionSetList)
    {
        if (selectionSetList.Count > 512)
        {
            return;
        }

        selectionSetList.Clear();
        Interlocked.Exchange(ref _temp, selectionSetList);
    }

    public ISelectionSetIndex Build()
        => new SelectionSetIndex(_selectionSets, _clonedToOriginalMap, _nextId);

    public SelectionSetIndexBuilder ToBuilder()
        => this;
}

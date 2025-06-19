using System.Collections;

namespace HotChocolate.Fusion.Execution;

internal class IncludeConditionCollection : ICollection<IncludeCondition>
{
    private readonly OrderedDictionary<IncludeCondition, bool> _dictionary = [];

    public IncludeCondition this[int index]
        => _dictionary.GetAt(index).Key;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    public bool Add(IncludeCondition item)
        => _dictionary.TryAdd(item, true);

    void ICollection<IncludeCondition>.Add(IncludeCondition item)
        => Add(item);

    public bool Remove(IncludeCondition item)
        => throw new InvalidOperationException("This is an add only collection.");

    void ICollection<IncludeCondition>.Clear()
        => throw new InvalidOperationException("This is an add only collection.");

    public bool Contains(IncludeCondition item)
        => _dictionary.ContainsKey(item);

    public int IndexOf(IncludeCondition item)
       => _dictionary.IndexOf(item);

    public void CopyTo(IncludeCondition[] array, int arrayIndex)
        => _dictionary.Keys.CopyTo(array, arrayIndex);

    public IEnumerator<IncludeCondition> GetEnumerator()
        => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

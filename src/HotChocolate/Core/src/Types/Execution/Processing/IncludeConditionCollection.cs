using System.Collections;

namespace HotChocolate.Execution.Processing;

internal sealed class IncludeConditionCollection : ICollection<IncludeCondition>
{
    private readonly OrderedDictionary<IncludeCondition, int> _dictionary = [];

    public IncludeCondition this[int index]
        => _dictionary.GetAt(index).Key;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    public bool Add(IncludeCondition item)
    {
        if (_dictionary.Count == 64)
        {
            throw new InvalidOperationException(
                "The maximum number of include conditions has been reached.");
        }

        return _dictionary.TryAdd(item, _dictionary.Count);
    }

    void ICollection<IncludeCondition>.Add(IncludeCondition item)
        => Add(item);

    public bool Remove(IncludeCondition item)
        => throw new InvalidOperationException("This is an add only collection.");

    void ICollection<IncludeCondition>.Clear()
        => throw new InvalidOperationException("This is an add only collection.");

    public bool Contains(IncludeCondition item)
        => _dictionary.ContainsKey(item);

    public int IndexOf(IncludeCondition item)
        => _dictionary.GetValueOrDefault(item, -1);

    public void CopyTo(IncludeCondition[] array, int arrayIndex)
        => _dictionary.Keys.CopyTo(array, arrayIndex);

    public IEnumerator<IncludeCondition> GetEnumerator()
        => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

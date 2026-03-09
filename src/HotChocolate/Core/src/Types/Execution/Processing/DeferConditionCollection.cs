using System.Collections;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferConditionCollection : ICollection<DeferCondition>
{
    private readonly OrderedDictionary<DeferCondition, int> _dictionary = [];

    public DeferCondition this[int index]
        => _dictionary.GetAt(index).Key;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    public bool Add(DeferCondition item)
    {
        if (_dictionary.Count == 64)
        {
            throw new InvalidOperationException(
                "The maximum number of defer conditions has been reached.");
        }

        return _dictionary.TryAdd(item, _dictionary.Count);
    }

    void ICollection<DeferCondition>.Add(DeferCondition item)
        => Add(item);

    public bool Remove(DeferCondition item)
        => throw new InvalidOperationException("This is an add only collection.");

    void ICollection<DeferCondition>.Clear()
        => throw new InvalidOperationException("This is an add only collection.");

    public bool Contains(DeferCondition item)
        => _dictionary.ContainsKey(item);

    public int IndexOf(DeferCondition item)
        => _dictionary.GetValueOrDefault(item, -1);

    public void CopyTo(DeferCondition[] array, int arrayIndex)
        => _dictionary.Keys.CopyTo(array, arrayIndex);

    public IEnumerator<DeferCondition> GetEnumerator()
        => _dictionary.Keys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}

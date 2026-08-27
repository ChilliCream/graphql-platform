using System.Collections;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferConditionCollection : ICollection<DeferCondition>
{
    /// <summary>
    /// The default maximum number of defer conditions an operation may declare.
    /// </summary>
    public const int DefaultMaxAllowedConditions = 1024;

    private readonly OrderedDictionary<DeferCondition, int> _dictionary = [];
    private readonly int _maxAllowedConditions;

    public DeferConditionCollection(int maxAllowedConditions = DefaultMaxAllowedConditions)
    {
        _maxAllowedConditions = maxAllowedConditions;
    }

    public DeferCondition this[int index]
        => _dictionary.GetAt(index).Key;

    public int Count => _dictionary.Count;

    public bool IsReadOnly => false;

    public bool Add(DeferCondition item)
    {
        if (!_dictionary.TryAdd(item, _dictionary.Count))
        {
            return false;
        }

        if (_dictionary.Count > _maxAllowedConditions)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(
                        "The operation exceeds the maximum allowed number of "
                        + $"defer conditions ({_maxAllowedConditions}).")
                    .Build());
        }

        return true;
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

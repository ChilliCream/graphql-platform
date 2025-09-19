using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class ObjectTypeDefinitionCollection
    : IList<MutableObjectTypeDefinition>
    , IReadOnlyObjectTypeDefinitionCollection
{
    private readonly List<MutableObjectTypeDefinition> _types = [];

    public MutableObjectTypeDefinition this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _types[index];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentNullException.ThrowIfNull(value);

            _types[index] = value;
        }
    }

    IObjectTypeDefinition IReadOnlyList<IObjectTypeDefinition>.this[int index]
        => this[index];

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public bool Contains(MutableObjectTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _types.Contains(item);
    }

    public void Add(MutableObjectTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _types.Add(item);
    }

    public bool Remove(MutableObjectTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _types.Remove(item);
    }

    public bool ContainsName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _types.Exists(t => t.Name.Equals(name));
    }

    public int IndexOf(MutableObjectTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _types.IndexOf(item);
    }

    public void Insert(int index, MutableObjectTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _types.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _types.RemoveAt(index);
    }

    public void Clear()
        => _types.Clear();

    public void CopyTo(MutableObjectTypeDefinition[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        _types.CopyTo(array, arrayIndex);
    }

    public IEnumerable<MutableObjectTypeDefinition> AsEnumerable()
        => _types;

    public IEnumerator<MutableObjectTypeDefinition> GetEnumerator()
        => _types.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();
}

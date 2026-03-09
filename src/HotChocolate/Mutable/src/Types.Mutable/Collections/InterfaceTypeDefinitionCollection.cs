using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class InterfaceTypeDefinitionCollection
    : IList<MutableInterfaceTypeDefinition>
    , IReadOnlyInterfaceTypeDefinitionCollection
{
    private readonly List<MutableInterfaceTypeDefinition> _interfaces = [];

    public int Count => _interfaces.Count;

    public bool IsReadOnly => false;

    public MutableInterfaceTypeDefinition this[int index]
    {
        get
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            return _interfaces[index];
        }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentNullException.ThrowIfNull(value);

            _interfaces[index] = value;
        }
    }

    IInterfaceTypeDefinition IReadOnlyList<IInterfaceTypeDefinition>.this[int index]
        => this[index];

    public void Add(MutableInterfaceTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _interfaces.Add(item);
    }

    public bool Remove(MutableInterfaceTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _interfaces.Remove(item);
    }

    public int IndexOf(MutableInterfaceTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _interfaces.IndexOf(item);
    }

    public void Insert(int index, MutableInterfaceTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _interfaces.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        _interfaces.RemoveAt(index);
    }

    public bool ContainsName(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        return _interfaces.Exists(t => t.Name.Equals(name));
    }

    public bool Contains(MutableInterfaceTypeDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return _interfaces.Contains(item);
    }

    public void Clear()
        => _interfaces.Clear();

    public void CopyTo(MutableInterfaceTypeDefinition[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);

        _interfaces.CopyTo(array, arrayIndex);
    }

    public IEnumerable<MutableInterfaceTypeDefinition> AsEnumerable()
        => _interfaces;

    public IEnumerator<MutableInterfaceTypeDefinition> GetEnumerator()
        => _interfaces.GetEnumerator();

    IEnumerator<IInterfaceTypeDefinition> IEnumerable<IInterfaceTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

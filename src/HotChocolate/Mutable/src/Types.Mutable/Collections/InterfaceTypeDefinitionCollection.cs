using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class InterfaceTypeDefinitionCollection
    : IList<MutableInterfaceTypeDefinition>
    , IReadOnlyInterfaceTypeDefinitionCollection
{
    private readonly List<MutableInterfaceTypeDefinition> _interfaces = new();

    public int Count => _interfaces.Count;

    public bool IsReadOnly => false;

    public MutableInterfaceTypeDefinition this[int index]
    {
        get => _interfaces[index];
        set => _interfaces[index] = value;
    }

    public void Add(MutableInterfaceTypeDefinition item)
        => _interfaces.Add(item);

    public bool Remove(MutableInterfaceTypeDefinition item)
        => _interfaces.Remove(item);

    public int IndexOf(MutableInterfaceTypeDefinition item)
        => _interfaces.IndexOf(item);

    public void Insert(int index, MutableInterfaceTypeDefinition item)
        => _interfaces.Insert(index, item);

    public void RemoveAt(int index)
        => _interfaces.RemoveAt(index);

    public void Clear()
        => _interfaces.Clear();

    public bool ContainsName(string name)
        => _interfaces.Exists(t => t.Name.Equals(name));

    public bool Contains(MutableInterfaceTypeDefinition item)
        => _interfaces.Contains(item);

    public void CopyTo(MutableInterfaceTypeDefinition[] array, int arrayIndex)
        => _interfaces.CopyTo(array, arrayIndex);

    public IEnumerable<MutableInterfaceTypeDefinition> AsEnumerable()
        => _interfaces;

    public IEnumerator<MutableInterfaceTypeDefinition> GetEnumerator()
        => _interfaces.GetEnumerator();

    IEnumerator<IInterfaceTypeDefinition> IEnumerable<IInterfaceTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

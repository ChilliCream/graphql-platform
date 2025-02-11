using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class InterfaceTypeDefinitionCollection
    : IList<InterfaceTypeDefinition>
    , IReadOnlyInterfaceTypeDefinitionCollection
{
    private readonly List<InterfaceTypeDefinition> _interfaces = new();

    public int Count => _interfaces.Count;

    public bool IsReadOnly => false;

    public InterfaceTypeDefinition this[int index]
    {
        get => _interfaces[index];
        set => _interfaces[index] = value;
    }

    public void Add(InterfaceTypeDefinition item)
        => _interfaces.Add(item);

    public bool Remove(InterfaceTypeDefinition item)
        => _interfaces.Remove(item);

    public int IndexOf(InterfaceTypeDefinition item)
        => _interfaces.IndexOf(item);

    public void Insert(int index, InterfaceTypeDefinition item)
        => _interfaces.Insert(index, item);

    public void RemoveAt(int index)
        => _interfaces.RemoveAt(index);

    public void Clear()
        => _interfaces.Clear();

    public bool ContainsName(string name)
        => _interfaces.Exists(t => t.Name.Equals(name));

    public bool Contains(InterfaceTypeDefinition item)
        => _interfaces.Contains(item);

    public void CopyTo(InterfaceTypeDefinition[] array, int arrayIndex)
        => _interfaces.CopyTo(array, arrayIndex);

    public IEnumerator<InterfaceTypeDefinition> GetEnumerator()
        => _interfaces.GetEnumerator();

    IEnumerator<IInterfaceTypeDefinition> IEnumerable<IInterfaceTypeDefinition>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

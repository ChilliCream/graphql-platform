using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class ObjectTypeDefinitionCollection
    : IList<MutableObjectTypeDefinition>
    , IReadOnlyObjectTypeDefinitionCollection
{
    private readonly List<MutableObjectTypeDefinition> _types = [];

    public MutableObjectTypeDefinition this[int index]
    {
        get => _types[index];
        set => _types[index] = value;
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public bool Contains(MutableObjectTypeDefinition item)
        => _types.Contains(item);

    public void Add(MutableObjectTypeDefinition item)
        => _types.Add(item);

    public bool Remove(MutableObjectTypeDefinition item)
        => _types.Remove(item);

    public bool ContainsName(string name)
        => _types.Exists(t => t.Name.Equals(name));

    public int IndexOf(MutableObjectTypeDefinition item)
        => _types.IndexOf(item);

    public void Insert(int index, MutableObjectTypeDefinition item)
        => _types.Insert(index, item);

    public void RemoveAt(int index)
        => _types.RemoveAt(index);

    public void Clear()
        => _types.Clear();

    public void CopyTo(MutableObjectTypeDefinition[] array, int arrayIndex)
        => _types.CopyTo(array, arrayIndex);

    public IEnumerable<MutableObjectTypeDefinition> AsEnumerable()
        => _types;

    public IEnumerator<MutableObjectTypeDefinition> GetEnumerator()
        => _types.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();
}

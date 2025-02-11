using System.Collections;

namespace HotChocolate.Types.Mutable;

public sealed class ObjectTypeDefinitionCollection
    : IList<ObjectTypeDefinition>
    , IReadOnlyObjectTypeDefinitionCollection
{
    private readonly List<ObjectTypeDefinition> _types = [];

    public ObjectTypeDefinition this[int index]
    {
        get => _types[index];
        set => _types[index] = value;
    }

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public bool Contains(ObjectTypeDefinition item)
        => _types.Contains(item);

    public void Add(ObjectTypeDefinition item)
        => _types.Add(item);

    public bool Remove(ObjectTypeDefinition item)
        => _types.Remove(item);

    public bool ContainsName(string name)
        => _types.Exists(t => t.Name.Equals(name));

    public int IndexOf(ObjectTypeDefinition item)
        => _types.IndexOf(item);

    public void Insert(int index, ObjectTypeDefinition item)
        => _types.Insert(index, item);

    public void RemoveAt(int index)
        => _types.RemoveAt(index);

    public void Clear()
        => _types.Clear();

    public void CopyTo(ObjectTypeDefinition[] array, int arrayIndex)
        => _types.CopyTo(array, arrayIndex);

    public IEnumerator<ObjectTypeDefinition> GetEnumerator()
        => _types.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    IEnumerator<IObjectTypeDefinition> IEnumerable<IObjectTypeDefinition>.GetEnumerator()
        => GetEnumerator();
}

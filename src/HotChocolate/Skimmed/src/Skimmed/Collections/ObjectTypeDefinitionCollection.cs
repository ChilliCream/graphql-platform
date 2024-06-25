using System.Collections;

namespace HotChocolate.Skimmed;

public sealed class ObjectTypeDefinitionCollection : IObjectTypeDefinitionCollection
{
    private readonly List<ObjectTypeDefinition> _types = [];

    public ObjectTypeDefinition this[int index] => _types[index];

    public int Count => _types.Count;

    public bool IsReadOnly => false;

    public bool Contains(ObjectTypeDefinition item)
        => _types.Contains(item);

    public void Add(ObjectTypeDefinition item)
        => _types.Add(item);

    public bool Remove(ObjectTypeDefinition item)
        => _types.Remove(item);

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
}

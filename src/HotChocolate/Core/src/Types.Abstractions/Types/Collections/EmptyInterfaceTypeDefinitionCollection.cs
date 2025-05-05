using System.Collections;

namespace HotChocolate.Types;

internal sealed class EmptyInterfaceTypeDefinitionCollection : IReadOnlyInterfaceTypeDefinitionCollection
{
    private EmptyInterfaceTypeDefinitionCollection() { }

    public int Count => 0;

    public IInterfaceTypeDefinition this[int index]
        => throw new ArgumentException(
            "The collection is empty.",
            nameof(index));

    public bool ContainsName(string name)
        => false;

    public IEnumerator<IInterfaceTypeDefinition> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static EmptyInterfaceTypeDefinitionCollection Instance { get; } = new();
}

using System.Collections;

#pragma warning disable IDE0130 // Namespace does not match folder structure
// ReSharper disable once CheckNamespace
namespace HotChocolate.Types;
#pragma warning restore IDE0130 // Namespace does not match folder structure

internal sealed class EmptyInterfaceTypeDefinitionCollection : IReadOnlyInterfaceTypeDefinitionCollection
{
    private EmptyInterfaceTypeDefinitionCollection() { }

    public int Count => 0;

    public IInterfaceTypeDefinition this[int index]
        => throw new ArgumentException("The collection is empty.", nameof(index));

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

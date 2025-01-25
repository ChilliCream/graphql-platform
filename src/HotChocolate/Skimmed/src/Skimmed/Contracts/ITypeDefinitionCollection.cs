using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface ITypeDefinitionCollection : ICollection<INamedTypeDefinition>
{
    INamedTypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? definition);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition;

    void Insert(int index, INamedTypeDefinition definition);

    bool Remove(string name);

    void RemoveAt(int index);

    bool ContainsName(string name);

    int IndexOf(INamedTypeDefinition definition);

    int IndexOf(string name);
}

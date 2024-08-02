using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Skimmed;

public interface ITypeDefinitionCollection : ICollection<INamedTypeDefinition>
{
    INamedTypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? type);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition;

    bool ContainsName(string name);
}

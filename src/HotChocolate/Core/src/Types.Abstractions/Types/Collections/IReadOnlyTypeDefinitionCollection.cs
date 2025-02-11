using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyTypeDefinitionCollection : IEnumerable<INamedTypeDefinition>
{
    INamedTypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? definition);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition;

    bool ContainsName(string name);
}

using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;

namespace HotChocolate;

public interface IReadOnlyTypeDefinitionCollection : IEnumerable<INamedTypeDefinition>
{
    INamedTypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out INamedTypeDefinition? definition);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : INamedTypeDefinition;

    bool ContainsName(string name);
}

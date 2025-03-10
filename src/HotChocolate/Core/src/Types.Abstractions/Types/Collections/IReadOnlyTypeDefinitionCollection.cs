using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyTypeDefinitionCollection : IEnumerable<ITypeDefinition>
{
    ITypeDefinition this[string name] { get; }

    bool TryGetType(string name, [NotNullWhen(true)] out ITypeDefinition? definition);

    bool TryGetType<T>(string name, [NotNullWhen(true)] out T? type) where T : ITypeDefinition;

    bool ContainsName(string name);
}

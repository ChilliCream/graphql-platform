using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Types;

public interface IReadOnlyInterfaceTypeDefinitionCollection
    : IEnumerable<IReadOnlyInterfaceTypeDefinition>
{
    bool ContainsName(string name);
}

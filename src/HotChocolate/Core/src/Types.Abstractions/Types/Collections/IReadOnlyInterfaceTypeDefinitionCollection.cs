namespace HotChocolate.Types;

public interface IReadOnlyInterfaceTypeDefinitionCollection
    : IEnumerable<IInterfaceTypeDefinition>
{
    bool ContainsName(string name);
}

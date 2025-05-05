namespace HotChocolate.Types;

public interface IReadOnlyInterfaceTypeDefinitionCollection
    : IReadOnlyList<IInterfaceTypeDefinition>
{
    bool ContainsName(string name);
}

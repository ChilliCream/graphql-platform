namespace HotChocolate.Types;

public interface IReadOnlyInterfaceTypeDefinitionCollection : IReadOnlyList<IReadOnlyInterfaceTypeDefinition>
{
    bool ContainsName(string name);
}

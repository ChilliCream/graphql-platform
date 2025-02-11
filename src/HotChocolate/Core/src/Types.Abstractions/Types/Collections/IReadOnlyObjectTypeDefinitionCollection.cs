namespace HotChocolate.Types;

public interface IReadOnlyObjectTypeDefinitionCollection
    : IEnumerable<IObjectTypeDefinition>
{
    bool ContainsName(string name);
}

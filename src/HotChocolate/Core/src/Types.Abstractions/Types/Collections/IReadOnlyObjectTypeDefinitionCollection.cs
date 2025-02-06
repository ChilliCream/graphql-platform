namespace HotChocolate.Types;

public interface IReadOnlyObjectTypeDefinitionCollection
    : IEnumerable<IReadOnlyObjectTypeDefinition>
{
    bool ContainsName(string name);
}

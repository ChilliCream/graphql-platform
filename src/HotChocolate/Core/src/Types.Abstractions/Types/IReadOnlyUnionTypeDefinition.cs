namespace HotChocolate.Types;

public interface IReadOnlyUnionTypeDefinition : IReadOnlyNamedTypeDefinition
{
    IReadOnlyObjectTypeDefinitionCollection Types { get; }
}

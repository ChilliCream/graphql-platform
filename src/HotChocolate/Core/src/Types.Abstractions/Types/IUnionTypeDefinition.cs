namespace HotChocolate.Types;

public interface IUnionTypeDefinition : INamedTypeDefinition
{
    IReadOnlyObjectTypeDefinitionCollection Types { get; }
}

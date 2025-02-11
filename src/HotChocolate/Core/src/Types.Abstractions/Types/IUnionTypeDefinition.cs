namespace HotChocolate.Types;

public interface IUnionTypeDefinition : ITypeDefinition
{
    IReadOnlyObjectTypeDefinitionCollection Types { get; }
}

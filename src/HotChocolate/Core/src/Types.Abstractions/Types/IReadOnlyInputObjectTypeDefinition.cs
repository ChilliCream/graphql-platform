namespace HotChocolate.Types;

public interface IReadOnlyInputObjectTypeDefinition : IReadOnlyNamedTypeDefinition
{
    IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition> Fields { get; }
}

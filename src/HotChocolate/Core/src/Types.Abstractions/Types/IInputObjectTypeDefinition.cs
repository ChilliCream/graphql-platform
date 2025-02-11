namespace HotChocolate.Types;

public interface IInputObjectTypeDefinition : INamedTypeDefinition
{
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Fields { get; }
}

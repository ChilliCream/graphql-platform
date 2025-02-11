namespace HotChocolate.Types;

public interface IInputObjectTypeDefinition : ITypeDefinition
{
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Fields { get; }
}

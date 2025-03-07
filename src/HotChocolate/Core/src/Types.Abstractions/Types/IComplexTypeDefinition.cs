namespace HotChocolate.Types;

public interface IComplexTypeDefinition : ITypeDefinition
{
    IReadOnlyInterfaceTypeDefinitionCollection Implements { get; }

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> Fields { get; }
}

namespace HotChocolate.Types;

public interface IComplexTypeDefinition : INamedTypeDefinition
{
    IReadOnlyInterfaceTypeDefinitionCollection Implements { get; }

    IReadOnlyFieldDefinitionCollection<IOutputFieldDefinition> Fields { get; }
}

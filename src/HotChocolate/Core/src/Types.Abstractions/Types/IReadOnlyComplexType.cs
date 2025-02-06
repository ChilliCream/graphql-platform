namespace HotChocolate.Types;

public interface IReadOnlyComplexType : IReadOnlyNamedTypeDefinition
{
    IReadOnlyInterfaceTypeDefinitionCollection Implements { get; }

    IReadOnlyFieldDefinitionCollection<IReadOnlyOutputFieldDefinition> Fields { get; }
}

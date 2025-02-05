namespace HotChocolate.Types;

public interface IReadOnlyComplexType : IReadOnlyNamedTypeDefinition
{
    public IReadOnlyInterfaceTypeDefinitionCollection Implements { get; }

    public IReadOnlyFieldDefinitionCollection<IReadOnlyOutputFieldDefinition> Fields { get; }
}

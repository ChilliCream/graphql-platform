namespace HotChocolate.Types;

public interface IOutputFieldDefinition : IFieldDefinition
{
    new IComplexTypeDefinition DeclaringType { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }
}

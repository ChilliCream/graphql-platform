namespace HotChocolate.Types;

public interface IOutputFieldDefinition : IFieldDefinition
{
    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }
}

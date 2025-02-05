namespace HotChocolate.Types;

public interface IReadOnlyOutputFieldDefinition : IReadOnlyFieldDefinition
{
    IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition> Arguments { get; }
}

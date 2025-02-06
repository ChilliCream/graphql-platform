namespace HotChocolate.Types;

public interface IReadOnlyDirectiveDefinition
{
    string Name { get; }

    string? Description { get; }

    bool IsRepeatable { get; }

    IReadOnlyFieldDefinitionCollection<IReadOnlyInputValueDefinition> Arguments { get; }

    DirectiveLocation Locations { get; }
}

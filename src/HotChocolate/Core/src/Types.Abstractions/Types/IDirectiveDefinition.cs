namespace HotChocolate.Types;

public interface IDirectiveDefinition
    : INameProvider
    , IDescriptionProvider
    , ISyntaxNodeProvider
{
    bool IsRepeatable { get; }

    IReadOnlyFieldDefinitionCollection<IInputValueDefinition> Arguments { get; }

    DirectiveLocation Locations { get; }
}

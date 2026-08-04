using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

public sealed class RequiresOptInDefinitionTests
{
    // The @requiresOptIn directive is repeatable, accepts a single required String
    // argument named "feature", and is valid on field definitions, argument definitions,
    // enum values, input field definitions, and directive definitions.
    [Fact]
    public void Create_ReturnsCorrectDefinition()
    {
        // arrange
        var schema = new MutableSchemaDefinition();

        // act
        var definition = RequiresOptInMutableDirectiveDefinition.Create(schema);

        // assert
        definition.ToString().MatchInlineSnapshot(
            """
            directive @requiresOptIn(feature: String!) repeatable on
              | FIELD_DEFINITION
              | ARGUMENT_DEFINITION
              | ENUM_VALUE
              | INPUT_FIELD_DEFINITION
              | DIRECTIVE_DEFINITION
            """);
    }
}

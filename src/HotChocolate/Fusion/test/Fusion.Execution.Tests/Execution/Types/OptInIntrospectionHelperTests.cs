using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Introspection;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class OptInIntrospectionHelperTests
{
    // A field with no @requiresOptIn directive is always visible, regardless of which
    // opt-in features the caller has enabled.
    [Fact]
    public void IsIncluded_NoRequiresOptIn_ReturnsTrue()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                field: String
            }
            """);

        var field = schema.QueryType.Fields["field"];

        // act
        var included = OptInIntrospectionHelper.IsIncluded(field.Directives, []);

        // assert
        Assert.True(included);
    }

    // A field annotated with @requiresOptIn is excluded when the caller has not enabled
    // the required feature.
    [Fact]
    public void IsIncluded_OptInNotRequested_ReturnsFalse()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                field: String @requiresOptIn(feature: "alpha")
            }

            directive @requiresOptIn(feature: String!) repeatable on
                | FIELD_DEFINITION
                | ARGUMENT_DEFINITION
                | ENUM_VALUE
                | INPUT_FIELD_DEFINITION
                | DIRECTIVE_DEFINITION
            """);

        var field = schema.QueryType.Fields["field"];

        // act
        var included = OptInIntrospectionHelper.IsIncluded(field.Directives, []);

        // assert
        Assert.False(included);
    }

    // A field annotated with @requiresOptIn is included when the caller has enabled
    // at least one of the required features.
    [Fact]
    public void IsIncluded_OptInRequested_ReturnsTrue()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                field: String @requiresOptIn(feature: "alpha")
            }

            directive @requiresOptIn(feature: String!) repeatable on
                | FIELD_DEFINITION
                | ARGUMENT_DEFINITION
                | ENUM_VALUE
                | INPUT_FIELD_DEFINITION
                | DIRECTIVE_DEFINITION
            """);

        var field = schema.QueryType.Fields["field"];

        // act
        var included = OptInIntrospectionHelper.IsIncluded(field.Directives, ["alpha"]);

        // assert
        Assert.True(included);
    }

    private static FusionSchemaDefinition ComposeSchema(string sourceSchemaText)
    {
        var sourceSchemas = new[] { new SourceSchemaText("a", sourceSchemaText) };
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sourceSchemas, composerOptions, compositionLog);

        var result = composer.Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}

using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Definitions;

public sealed class ContextDefinitionTests
{
    [Fact]
    public void Create_Should_ReturnRepeatableDirectiveOnObjectInterfaceAndUnion_When_Constructed()
    {
        // arrange
        var stringType = BuiltIns.String.Create();

        // act
        var definition = new ContextMutableDirectiveDefinition(stringType);

        // assert
        definition.ToString().MatchInlineSnapshot(
            """
            "The @context directive declares that the values of a type set a named context."
            directive @context(
              "The name of the context that the annotated type sets."
              name: String!
            ) repeatable on OBJECT | INTERFACE | UNION
            """);
    }

    [Fact]
    public void SourceSchema_Should_PassParsingAndValidation_When_ContextIsUsedWithoutDeclaration()
    {
        // arrange
        var sourceSchemaText = new SourceSchemaText(
            "A",
            // lang=graphql
            """
            type Query {
                user: User
                node: Node
                result: Result
            }

            type User @context(name: "userContext") {
                id: ID!
            }

            interface Node @context(name: "userContext") {
                id: ID!
            }

            union Result @context(name: "userContext") = User
            """);
        var log = new CompositionLog();
        var parseResult = new SourceSchemaParser(sourceSchemaText, log).Parse();
        Assert.True(parseResult.IsSuccess);
        var schema = parseResult.Value;
        new SourceSchemaPreprocessor(schema, [schema], log).Preprocess();
        new SourceSchemaEnricher(schema, [schema]).Enrich();
        var validator = new SourceSchemaValidator(
            [schema],
            SchemaComposer.SourceSchemaRules,
            log);

        // act
        var validationResult = validator.Validate();

        // assert
        Assert.True(validationResult.IsSuccess);
        Assert.True(log.IsEmpty);
    }
}

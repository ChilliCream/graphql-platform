using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionConnectorKindTests
{
    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnApollo_When_DirectiveStampedAsApollo()
    {
        // arrange
        var schema = ComposeSchema(
            ("Products",
             """
             schema @fusion__connector(kind: "Apollo") {
               query: Query
             }

             type Query {
               productById(id: ID!): Product @lookup
             }

             type Product @key(fields: "id") {
               id: ID!
             }
             """));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Products");

        // assert
        Assert.Equal("Apollo", kind);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnNull_When_DirectiveAbsent()
    {
        // arrange
        var schema = ComposeSchema(
            ("Catalog",
             """
             type Query {
               ping: String
             }
             """));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Catalog");

        // assert
        Assert.Null(kind);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnNull_When_DirectiveStampedAsGraphQL()
    {
        // arrange
        var schema = ComposeSchema(
            ("Catalog",
             """
             schema @fusion__connector(kind: "GraphQL") {
               query: Query
             }

             type Query {
               ping: String
             }
             """));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Catalog");

        // assert
        Assert.Null(kind);
    }

    [Fact]
    public void GetSourceSchemaConnectorKind_Should_ReturnNull_When_NameIsNotASourceSchema()
    {
        // arrange
        var schema = ComposeSchema(
            ("Products",
             """
             schema @fusion__connector(kind: "Apollo") {
               query: Query
             }

             type Query {
               productById(id: ID!): Product @lookup
             }

             type Product @key(fields: "id") {
               id: ID!
             }
             """));

        // act
        var kind = schema.GetSourceSchemaConnectorKind("Unknown");

        // assert
        Assert.Null(kind);
    }

    private static FusionSchemaDefinition ComposeSchema(params (string Name, string Sdl)[] sources)
    {
        var sourceTexts = sources.Select(s => new SourceSchemaText(s.Name, s.Sdl)).ToArray();
        var compositionLog = new CompositionLog();
        var composerOptions = new SchemaComposerOptions();
        var composer = new SchemaComposer(sourceTexts, composerOptions, compositionLog);

        var result = composer.Compose();
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());
    }
}

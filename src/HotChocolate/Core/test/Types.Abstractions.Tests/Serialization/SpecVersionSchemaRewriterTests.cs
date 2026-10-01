using HotChocolate.Language;

namespace HotChocolate.Serialization;

public class SpecVersionSchemaRewriterTests
{
    [Fact]
    public void Rewrite_Should_Remove_Unsupported_Directive_Definition_Locations_When_October2021()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            directive @tag(name: String!) repeatable on OBJECT | FIELD_DEFINITION | DIRECTIVE_DEFINITION
            directive @requiresOptIn on OBJECT | DIRECTIVE_DEFINITION
            directive @onlyOnDirective on DIRECTIVE_DEFINITION
            directive @annotation on OBJECT

            type Query @tag(name: "query") @requiresOptIn @onlyOnDirective {
              value: String @tag(name: "value")
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.October2021);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            directive @tag(name: String!) repeatable on OBJECT | FIELD_DEFINITION

            directive @requiresOptIn on OBJECT

            directive @annotation on OBJECT

            type Query @tag(name: "query") @requiresOptIn {
              value: String @tag(name: "value")
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Remove_Directive_Definition_Directives_And_Extensions_When_September2025()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            directive @tag(name: String!) repeatable on OBJECT | DIRECTIVE_DEFINITION
            directive @annotated @tag(name: "definition") on OBJECT
            extend directive @tag @tag(name: "extension")

            type Query @tag(name: "query") {
              value: String
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.September2025);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            directive @tag(name: String!) repeatable on OBJECT

            directive @annotated on OBJECT

            type Query @tag(name: "query") {
              value: String
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Remove_OneOf_And_Fold_Deprecation_When_October2021()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            directive @oneOf on INPUT_OBJECT
            directive @deprecated(reason: String) on FIELD_DEFINITION | ENUM_VALUE | ARGUMENT_DEFINITION | INPUT_FIELD_DEFINITION

            input Filter @oneOf {
              "The old id."
              id: ID @deprecated(reason: "Use key")
              name: String @deprecated
            }

            type Query {
              search(
                legacy: Boolean @deprecated(reason: "Use filter")
                obsolete: Boolean @deprecated
              ): String
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.October2021);

        // assert
        result.ToString().MatchInlineSnapshot(
            """"
            input Filter {
              """
              The old id.

              Deprecated: Use key
              """
              id: ID
              "Deprecated: No longer supported"
              name: String
            }

            type Query {
              search(
                "Deprecated: Use filter"
                legacy: Boolean
                "Deprecated: No longer supported"
                obsolete: Boolean
              ): String
            }
            """");
    }

    [Fact]
    public void Rewrite_Should_Remove_Multiple_Unsupported_Directives_When_October2021()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            directive @keptBefore on ARGUMENT_DEFINITION
            directive @keptBetween on ARGUMENT_DEFINITION
            directive @onlyOnDirective on DIRECTIVE_DEFINITION
            directive @keptAfter on ARGUMENT_DEFINITION

            type Query {
              value(
                argument: String @keptBefore @deprecated(reason: "Use another argument") @keptBetween @onlyOnDirective @keptAfter
              ): String
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.October2021);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            directive @keptBefore on ARGUMENT_DEFINITION

            directive @keptBetween on ARGUMENT_DEFINITION

            directive @keptAfter on ARGUMENT_DEFINITION

            type Query {
              value(
                "Deprecated: Use another argument"
                argument: String @keptBefore @keptBetween @keptAfter
              ): String
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Preserve_OneOf_And_Deprecation_When_September2025()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            input Filter @oneOf {
              id: ID @deprecated(reason: "Use key")
            }

            type Query {
              search(legacy: Boolean @deprecated(reason: "Use filter")): String
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.September2025);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            input Filter @oneOf {
              id: ID @deprecated(reason: "Use key")
            }

            type Query {
              search(legacy: Boolean @deprecated(reason: "Use filter")): String
            }
            """);
    }

    [Theory]
    [InlineData(GraphQLSpecVersion.October2021)]
    [InlineData(GraphQLSpecVersion.September2025)]
    public void Rewrite_Should_Remove_Printed_Spec_Definitions_When_Version_IsRequested(
        GraphQLSpecVersion version)
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            scalar String
            scalar Int
            scalar Float
            scalar Boolean
            scalar ID
            scalar DateTime

            directive @skip(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
            directive @include(if: Boolean!) on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT
            directive @deprecated(reason: String) on FIELD_DEFINITION | ENUM_VALUE
            directive @specifiedBy(url: String!) on SCALAR
            directive @oneOf on INPUT_OBJECT

            type Query {
              value: DateTime
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, version);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            scalar DateTime

            type Query {
              value: DateTime
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Preserve_Custom_Directives_When_October2021()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            directive @semanticNonNull on FIELD_DEFINITION
            directive @cost on FIELD_DEFINITION
            directive @tag(name: String!) repeatable on OBJECT | FIELD_DEFINITION | DIRECTIVE_DEFINITION
            directive @defer on FIELD_DEFINITION
            directive @stream on FIELD_DEFINITION

            type Query @tag(name: "query") {
              value: String @semanticNonNull @cost @tag(name: "value") @defer @stream
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.October2021);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            directive @semanticNonNull on FIELD_DEFINITION

            directive @cost on FIELD_DEFINITION

            directive @tag(name: String!) repeatable on OBJECT | FIELD_DEFINITION

            directive @defer on FIELD_DEFINITION

            directive @stream on FIELD_DEFINITION

            type Query @tag(name: "query") {
              value: String @semanticNonNull @cost @tag(name: "value") @defer @stream
            }
            """);
    }

    [Fact]
    public void Rewrite_Should_Preserve_Document_Instance_When_Nothing_Changes()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              value: String
            }
            """);

        // act
        var result = SpecVersionSchemaRewriter.Rewrite(schema, GraphQLSpecVersion.September2025);

        // assert
        Assert.Same(schema, result);
    }

    [Fact]
    public void Rewrite_Should_Preserve_SemanticNonNull_When_Combined_With_October2021()
    {
        // arrange
        var schema = Utf8GraphQLParser.Parse(
            """
            type Query {
              value: String!
            }
            """);

        // act
        var semanticNonNullSchema = SemanticNonNullSchemaRewriter.Rewrite(schema);
        var result = SpecVersionSchemaRewriter.Rewrite(
            semanticNonNullSchema,
            GraphQLSpecVersion.October2021);

        // assert
        result.ToString().MatchInlineSnapshot(
            """
            type Query {
              value: String @semanticNonNull
            }

            directive @semanticNonNull(levels: [Int!] = [0]) on FIELD_DEFINITION
            """);
    }
}

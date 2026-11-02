using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Serialization;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionBuiltInDirectiveCompatibilityTests
{
    [Theory]
    [InlineData("fusion__costOptions")]
    [InlineData("fusion__cost_options")]
    public void Create_Should_ParseCostOptions_When_DirectiveUsesSupportedName(string directiveName)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            $$"""
            directive @{{directiveName}}(defaultListSize: Int) on SCHEMA

            schema @{{directiveName}}(defaultListSize: 42) {
                query: Query
            }

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        // act
        var schema = FusionSchemaDefinition.Create(document);

        // assert
        Assert.Equal(42, schema.DefaultListSize);
    }

    [Theory]
    [InlineData("fusion__gatewayField")]
    [InlineData("fusion__gateway_field")]
    public void Create_Should_MarkFieldAsGatewayField_When_DirectiveUsesSupportedName(string directiveName)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            $$"""
            directive @{{directiveName}} on FIELD_DEFINITION

            type Query {
                node: String @{{directiveName}}
            }

            enum fusion__Schema {
                A
            }
            """);

        // act
        var schema = FusionSchemaDefinition.Create(document);

        // assert
        Assert.True(schema.Types.GetType<FusionObjectTypeDefinition>("Query").Fields["node"].IsGatewayField);
    }

    [Theory]
    [InlineData("fusion__schemaMetadata")]
    [InlineData("fusion__schema_metadata")]
    public void Create_Should_ReadSourceSchemaMetadata_When_DirectiveUsesSupportedName(string directiveName)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            $$"""
            directive @{{directiveName}}(
                name: String!
                kind: String
                allowNonResolvableInterfaceObjects: Boolean
            ) on ENUM_VALUE

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A @{{directiveName}}(
                    name: "source"
                    kind: "ApolloFederation"
                    allowNonResolvableInterfaceObjects: true)
            }
            """);

        // act
        var schema = FusionSchemaDefinition.Create(document);

        // assert
        Assert.Equal("ApolloFederation", schema.GetSourceSchemaConnectorKind("source"));
        Assert.True(schema.AllowsNonResolvableInterfaceObjects("source"));
    }

    [Theory]
    [InlineData("fusion__costOptions")]
    [InlineData("fusion__cost_options")]
    [InlineData("fusion__gatewayField")]
    [InlineData("fusion__gateway_field")]
    [InlineData("fusion__schemaMetadata")]
    [InlineData("fusion__schema_metadata")]
    public void Format_Should_HideBuiltInDirective_When_DirectiveUsesSupportedName(string directiveName)
    {
        // arrange
        var schema = FusionSchemaDefinition.Create(CreateDocument(directiveName));

        // act
        var document = SchemaFormatter.FormatAsDocument(
            schema,
            new SchemaFormatterOptions { IncludeInternalDirectives = false });

        // assert
        Assert.Equal(
            ["defer"],
            document.Definitions
                .OfType<DirectiveDefinitionNode>()
                .Select(static definition => definition.Name.Value));
        Assert.Empty(GetAppliedDirectiveNames(document, directiveName));
    }

    [Fact]
    public void Create_Should_RejectMixedCostOptionsDirectiveNames()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(
            """
            schema @fusion__costOptions(defaultListSize: 1) {
                query: Query
            }

            extend schema @fusion__cost_options(defaultListSize: 2)

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionSchemaDefinition.Create(document));

        // assert
        Assert.Equal(
            "The fusion__costOptions directive may only be applied once per schema.",
            exception.Message);
    }

    private static DocumentNode CreateDocument(string directiveName)
        => directiveName switch
        {
            "fusion__costOptions" or "fusion__cost_options" => Utf8GraphQLParser.Parse(
                $$"""
                directive @{{directiveName}}(defaultListSize: Int) on SCHEMA

                schema @{{directiveName}} {
                    query: Query
                }

                type Query {
                    ping: String
                }

                enum fusion__Schema {
                    A
                }
                """),
            "fusion__gatewayField" or "fusion__gateway_field" => Utf8GraphQLParser.Parse(
                $$"""
                directive @{{directiveName}} on FIELD_DEFINITION

                type Query {
                    node: String @{{directiveName}}
                }

                enum fusion__Schema {
                    A
                }
                """),
            "fusion__schemaMetadata" or "fusion__schema_metadata" => Utf8GraphQLParser.Parse(
                $$"""
                directive @{{directiveName}}(name: String!) on ENUM_VALUE

                type Query {
                    ping: String
                }

                enum fusion__Schema {
                    A @{{directiveName}}(name: "source")
                }
                """),
            _ => throw new ArgumentOutOfRangeException(nameof(directiveName))
        };

    private static IEnumerable<string> GetAppliedDirectiveNames(
        DocumentNode document,
        string directiveName)
        => directiveName switch
        {
            "fusion__costOptions" or "fusion__cost_options" => document.Definitions
                .OfType<SchemaDefinitionNode>()
                .SelectMany(static definition => definition.Directives)
                .Select(static directive => directive.Name.Value),
            "fusion__gatewayField" or "fusion__gateway_field" => document.Definitions
                .OfType<ObjectTypeDefinitionNode>()
                .Single(static definition => definition.Name.Value.Equals("Query", StringComparison.Ordinal))
                .Fields
                .SelectMany(static field => field.Directives)
                .Select(static directive => directive.Name.Value),
            "fusion__schemaMetadata" or "fusion__schema_metadata" => document.Definitions
                .OfType<EnumTypeDefinitionNode>()
                .SelectMany(static definition => definition.Values)
                .SelectMany(static value => value.Directives)
                .Select(static directive => directive.Name.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(directiveName))
        };
}

using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionNodeResolutionTests
{
    [Fact]
    public void Create_Should_DefaultToGateway_When_ExecutionDirectiveIsAbsent()
    {
        var schema = CreateSchema();

        Assert.Equal(NodeResolution.Gateway, schema.NodeResolution);
    }

    [Fact]
    public void Create_Should_DefaultToGateway_When_ArgumentIsOmitted()
    {
        var schema = CreateSchema("@fusion__execution");

        Assert.Equal(NodeResolution.Gateway, schema.NodeResolution);
    }

    [Theory]
    [InlineData("GATEWAY", NodeResolution.Gateway)]
    [InlineData("SOURCE_SCHEMA", NodeResolution.SourceSchema)]
    public void Create_Should_ParseNodeResolution_When_ArgumentIsExplicit(
        string value,
        NodeResolution expected)
    {
        var schema = CreateSchema($"@fusion__execution(nodeResolution: {value})");

        Assert.Equal(expected, schema.NodeResolution);
    }

    [Fact]
    public void Create_Should_ParseExecutionDirective_FromSchemaExtension()
    {
        var document = Utf8GraphQLParser.Parse(
            """
            schema {
                query: Query
            }

            extend schema @fusion__execution(nodeResolution: SOURCE_SCHEMA)

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        var schema = FusionSchemaDefinition.Create(document);

        Assert.Equal(NodeResolution.SourceSchema, schema.NodeResolution);
    }

    [Fact]
    public void Create_Should_RejectDuplicateExecutionDirectives()
    {
        var document = Utf8GraphQLParser.Parse(
            """
            schema @fusion__execution(nodeResolution: GATEWAY) {
                query: Query
            }

            extend schema @fusion__execution(nodeResolution: SOURCE_SCHEMA)

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionSchemaDefinition.Create(document));

        Assert.Equal(
            "The fusion__execution directive may only be applied once per schema.",
            exception.Message);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("null")]
    [InlineData("\"SOURCE_SCHEMA\"")]
    public void Create_Should_RejectInvalidNodeResolution(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema($"@fusion__execution(nodeResolution: {value})"));

        Assert.Equal(
            "The fusion__execution nodeResolution argument must be GATEWAY or SOURCE_SCHEMA.",
            exception.Message);
    }

    private static FusionSchemaDefinition CreateSchema(string? executionDirective = null)
    {
        var document = Utf8GraphQLParser.Parse(
            $$"""
            schema {{executionDirective}} {
                query: Query
            }

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        return FusionSchemaDefinition.Create(document);
    }
}

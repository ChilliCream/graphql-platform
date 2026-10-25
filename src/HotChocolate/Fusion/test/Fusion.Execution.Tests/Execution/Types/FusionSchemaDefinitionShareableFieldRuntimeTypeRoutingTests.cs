using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionShareableFieldRuntimeTypeRoutingTests
{
    [Fact]
    public void Create_Should_DefaultToSourceLocal_When_ExecutionDirectiveIsAbsent()
    {
        var schema = CreateSchema();

        Assert.Equal(
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            schema.ShareableFieldRuntimeTypeRouting);
    }

    [Fact]
    public void Create_Should_DefaultToSourceLocal_When_ArgumentIsOmitted()
    {
        var schema = CreateSchema("@fusion__execution");

        Assert.Equal(
            ShareableFieldRuntimeTypeRouting.SourceLocal,
            schema.ShareableFieldRuntimeTypeRouting);
    }

    [Theory]
    [InlineData("SOURCE_LOCAL", ShareableFieldRuntimeTypeRouting.SourceLocal)]
    [InlineData("COMMON_RUNTIME_TYPES", ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes)]
    public void Create_Should_ParseRouting_When_ArgumentIsExplicit(
        string value,
        ShareableFieldRuntimeTypeRouting expected)
    {
        var schema = CreateSchema(
            $"@fusion__execution(shareableFieldRuntimeTypeRouting: {value})");

        Assert.Equal(expected, schema.ShareableFieldRuntimeTypeRouting);
    }

    [Fact]
    public void Create_Should_ParseRouting_FromSchemaExtension()
    {
        var document = Utf8GraphQLParser.Parse(
            """
            schema {
                query: Query
            }

            extend schema
                @fusion__execution(shareableFieldRuntimeTypeRouting: COMMON_RUNTIME_TYPES)

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        var schema = FusionSchemaDefinition.Create(document);

        Assert.Equal(
            ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            schema.ShareableFieldRuntimeTypeRouting);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("null")]
    [InlineData("\"COMMON_RUNTIME_TYPES\"")]
    public void Create_Should_RejectInvalidRouting(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema(
                $"@fusion__execution(shareableFieldRuntimeTypeRouting: {value})"));

        Assert.Equal(
            "The fusion__execution shareableFieldRuntimeTypeRouting argument must be "
            + "SOURCE_LOCAL or COMMON_RUNTIME_TYPES.",
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

using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionCostOptionsTests
{
    [Fact]
    public void Create_Should_LeaveDefaultListSizeUnbounded_When_CostOptionsDirectiveIsAbsent()
    {
        var schema = CreateSchema();

        Assert.Null(schema.DefaultListSize);
    }

    [Fact]
    public void Create_Should_LeaveDefaultListSizeUnbounded_When_ArgumentIsOmitted()
    {
        var schema = CreateSchema("@fusion__cost_options");

        Assert.Null(schema.DefaultListSize);
    }

    [Fact]
    public void Create_Should_LeaveDefaultListSizeUnbounded_When_ArgumentIsExplicitNull()
    {
        var schema = CreateSchema("@fusion__cost_options(defaultListSize: null)");

        Assert.Null(schema.DefaultListSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    public void Create_Should_ParseDefaultListSize_When_ArgumentIsANonNegativeInt(int value)
    {
        var schema = CreateSchema($"@fusion__cost_options(defaultListSize: {value})");

        Assert.Equal(value, schema.DefaultListSize);
    }

    [Fact]
    public void Create_Should_ParseCostOptionsDirective_FromSchemaExtension()
    {
        var document = Utf8GraphQLParser.Parse(
            """
            schema {
                query: Query
            }

            extend schema @fusion__cost_options(defaultListSize: 7)

            type Query {
                ping: String
            }

            enum fusion__Schema {
                A
            }
            """);

        var schema = FusionSchemaDefinition.Create(document);

        Assert.Equal(7, schema.DefaultListSize);
    }

    [Fact]
    public void Create_Should_RejectDuplicateCostOptionsDirectives()
    {
        var document = Utf8GraphQLParser.Parse(
            """
            schema @fusion__cost_options(defaultListSize: 1) {
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

        var exception = Assert.Throws<InvalidOperationException>(
            () => FusionSchemaDefinition.Create(document));

        Assert.Equal(
            "The fusion__cost_options directive may only be applied once per schema.",
            exception.Message);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("\"1\"")]
    [InlineData("1.5")]
    [InlineData("true")]
    [InlineData("99999999999")]
    public void Create_Should_RejectInvalidDefaultListSize(string value)
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema($"@fusion__cost_options(defaultListSize: {value})"));

        Assert.Equal(
            "The fusion__cost_options defaultListSize argument must be a non-negative integer.",
            exception.Message);
    }

    private static FusionSchemaDefinition CreateSchema(string? costOptionsDirective = null)
    {
        var document = Utf8GraphQLParser.Parse(
            $$"""
            schema {{costOptionsDirective}} {
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

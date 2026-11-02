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

    [Fact]
    public void Create_Should_LeaveCostSettingsUnset_When_ArgumentsAreOmitted()
    {
        // arrange
        // act
        var schema = CreateSchema("@fusion__cost_options");

        // assert
        new
        {
            schema.MaxFieldCost,
            schema.MaxTypeCost,
            schema.EnforceCostLimits,
            schema.SkipAnalyzer,
            schema.MaxResponseSize,
            schema.CostPlanCacheSize,
            schema.CaseBudget,
            schema.CaseBudgetExceededBehavior
        }.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": null,
              "MaxTypeCost": null,
              "EnforceCostLimits": null,
              "SkipAnalyzer": null,
              "MaxResponseSize": null,
              "CostPlanCacheSize": null,
              "CaseBudget": null,
              "CaseBudgetExceededBehavior": null
            }
            """);
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
    public void Create_Should_ParseCostSettings_When_AllArgumentsAreSpecified()
    {
        // arrange
        // act
        var schema = CreateSchema(
            """
            @fusion__cost_options(
              defaultListSize: 7
              maxFieldCost: 10.5
              maxTypeCost: 20.5
              enforceCostLimits: false
              skipAnalyzer: true
              maxResponseSize: 30.5
              costPlanCacheSize: 40
              caseBudget: -50
              caseBudgetExceededBehavior: OVERESTIMATE
            )
            """);

        // assert
        new
        {
            schema.DefaultListSize,
            schema.MaxFieldCost,
            schema.MaxTypeCost,
            schema.EnforceCostLimits,
            schema.SkipAnalyzer,
            schema.MaxResponseSize,
            schema.CostPlanCacheSize,
            schema.CaseBudget,
            schema.CaseBudgetExceededBehavior
        }.MatchInlineSnapshot(
            """
            {
              "DefaultListSize": 7,
              "MaxFieldCost": 10.5,
              "MaxTypeCost": 20.5,
              "EnforceCostLimits": false,
              "SkipAnalyzer": true,
              "MaxResponseSize": 30.5,
              "CostPlanCacheSize": 40,
              "CaseBudget": -50,
              "CaseBudgetExceededBehavior": "OVERESTIMATE"
            }
            """);
    }

    [Theory]
    [InlineData("EVALUATE_PER_REQUEST")]
    [InlineData("OVERESTIMATE")]
    public void Create_Should_ParseCaseBudgetExceededBehavior_When_ArgumentIsAValidEnumValue(
        string value)
    {
        // arrange

        // act
        var schema = CreateSchema($"@fusion__cost_options(caseBudgetExceededBehavior: {value})");

        // assert
        Assert.Equal(value, schema.CaseBudgetExceededBehavior);
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

    [Theory]
    [InlineData("maxFieldCost", "-1", "a non-negative finite number")]
    [InlineData("maxTypeCost", "-1", "a non-negative finite number")]
    [InlineData("maxResponseSize", "-1", "a non-negative finite number")]
    [InlineData("costPlanCacheSize", "0", "a positive integer")]
    [InlineData("enforceCostLimits", "1", "a Boolean")]
    [InlineData("caseBudget", "1.5", "an integer")]
    public void Create_Should_RejectInvalidCostSetting(
        string argumentName,
        string value,
        string requirement)
    {
        // arrange
        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema($"@fusion__cost_options({argumentName}: {value})"));

        // assert
        Assert.Equal(
            $"The fusion__cost_options {argumentName} argument must be {requirement}.",
            exception.Message);
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("\"EVALUATE_PER_REQUEST\"")]
    [InlineData("\"OVERESTIMATE\"")]
    public void Create_Should_RejectCaseBudgetExceededBehavior_When_ArgumentIsNotAValidEnumValue(
        string value)
    {
        // arrange

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema($"@fusion__cost_options(caseBudgetExceededBehavior: {value})"));

        // assert
        Assert.Equal(
            "The fusion__cost_options caseBudgetExceededBehavior argument must be "
            + "EVALUATE_PER_REQUEST or OVERESTIMATE.",
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

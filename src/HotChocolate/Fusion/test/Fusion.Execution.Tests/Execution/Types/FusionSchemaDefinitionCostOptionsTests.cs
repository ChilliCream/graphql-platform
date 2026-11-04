using System.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Types;

public sealed class FusionSchemaDefinitionCostOptionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("{}")]
    [InlineData("{\"cost\":{}}")]
    public void Create_Should_UseDefaults_When_SettingsAreMissing(string? settings)
    {
        // arrange
        // act
        var schema = CreateSchema(settings: settings);

        // assert
        schema.CostOptions.MatchInlineSnapshot(
            """
            {
              "DefaultListSize": null,
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

    [Fact]
    public void Create_Should_ReadCostSettings_When_SettingsAndDirectiveArePresent()
    {
        // arrange
        const string settings = """
            { "cost": {
              "maxFieldCost": 10.5, "maxTypeCost": 20.5,
              "enforceCostLimits": false, "skipAnalyzer": true,
              "maxResponseSize": 30.5, "costPlanCacheSize": 40,
              "caseBudget": -50, "caseBudgetExceededBehavior": "Overestimate"
            }}
            """;

        // act
        var schema = CreateSchema("@fusion__cost_options(defaultListSize: 7 maxFieldCost: -1)", settings);

        // assert
        new
        {
            schema.CostOptions.DefaultListSize,
            schema.CostOptions.MaxFieldCost,
            schema.CostOptions.MaxTypeCost,
            schema.CostOptions.EnforceCostLimits,
            schema.CostOptions.SkipAnalyzer,
            schema.CostOptions.MaxResponseSize,
            schema.CostOptions.CostPlanCacheSize,
            schema.CostOptions.CaseBudget,
            CaseBudgetExceededBehavior = schema.CostOptions.CaseBudgetExceededBehavior?.ToString()
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
              "CaseBudgetExceededBehavior": "Overestimate"
            }
            """);
    }

    [Theory]
    [InlineData("EvaluatePerRequest")]
    [InlineData("Overestimate")]
    public void Create_Should_ParseBehavior_When_EnumNameIsValid(string value)
    {
        // arrange
        var settings = $$"""{ "cost": { "caseBudgetExceededBehavior": "{{value}}" } }""";

        // act
        var schema = CreateSchema(settings: settings);

        // assert
        Assert.Equal(value, schema.CostOptions.CaseBudgetExceededBehavior?.ToString());
    }

    [Theory]
    [InlineData("@fusion__cost_options", null)]
    [InlineData("@fusion__cost_options(defaultListSize: null)", null)]
    [InlineData("@fusion__cost_options(defaultListSize: 0)", 0)]
    [InlineData("@fusion__cost_options(defaultListSize: 42)", 42)]
    public void Create_Should_ParseDefaultListSize_When_DirectiveIsPresent(string directive, int? expected)
    {
        // arrange
        // act
        var schema = CreateSchema(directive);

        // assert
        Assert.Equal(expected, schema.CostOptions.DefaultListSize);
    }

    [Fact]
    public void Create_Should_ReadDefaultListSize_When_DirectiveIsOnSchemaExtension()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("""
            schema { query: Query }
            extend schema @fusion__cost_options(defaultListSize: 7)
            type Query { ping: String }
            enum fusion__Schema { A }
            """);

        // act
        var schema = FusionSchemaDefinition.Create(document);

        // assert
        Assert.Equal(7, schema.CostOptions.DefaultListSize);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("\"1\"")]
    [InlineData("1.5")]
    [InlineData("true")]
    [InlineData("99999999999")]
    public void Create_Should_RejectInvalidDefaultListSize(string value)
    {
        // arrange
        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => CreateSchema($"@fusion__cost_options(defaultListSize: {value})"));

        // assert
        Assert.Equal("The fusion__cost_options defaultListSize argument must be a non-negative integer.", exception.Message);
    }

    [Theory]
    [InlineData("maxFieldCost", "-1", "a non-negative finite number")]
    [InlineData("maxTypeCost", "\"1\"", "a non-negative finite number")]
    [InlineData("maxResponseSize", "1e999", "a non-negative finite number")]
    [InlineData("costPlanCacheSize", "0", "a positive integer")]
    [InlineData("enforceCostLimits", "1", "a Boolean")]
    [InlineData("skipAnalyzer", "null", "a Boolean")]
    [InlineData("caseBudget", "1.5", "an integer")]
    [InlineData("caseBudgetExceededBehavior", "\"INVALID\"", "EvaluatePerRequest or Overestimate")]
    [InlineData("caseBudgetExceededBehavior", "1", "EvaluatePerRequest or Overestimate")]
    public void Create_Should_RejectInvalidSetting_WithJsonPath(string name, string value, string requirement)
    {
        // arrange
        var settings = $$"""{ "cost": { "{{name}}": {{value}} } }""";

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateSchema(settings: settings));

        // assert
        Assert.Equal($"The $.cost.{name} setting must be {requirement}.", exception.Message);
    }

    [Theory]
    [InlineData("[]", "$", "an object")]
    [InlineData("{\"cost\":null}", "$.cost", "an object")]
    public void Create_Should_RejectInvalidSettingsContainer(string settings, string path, string requirement)
    {
        // arrange
        // act
        var exception = Assert.Throws<InvalidOperationException>(() => CreateSchema(settings: settings));

        // assert
        Assert.Equal($"The {path} setting must be {requirement}.", exception.Message);
    }

    [Fact]
    public void Create_Should_RejectDuplicateCostOptionsDirectives()
    {
        // arrange
        var document = Utf8GraphQLParser.Parse("""
            schema @fusion__cost_options(defaultListSize: 1) { query: Query }
            extend schema @fusion__cost_options(defaultListSize: 2)
            type Query { ping: String }
            enum fusion__Schema { A }
            """);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => FusionSchemaDefinition.Create(document));

        // assert
        Assert.Equal("The fusion__cost_options directive may only be applied once per schema.", exception.Message);
    }

    private static FusionSchemaDefinition CreateSchema(string? directive = null, string? settings = null)
    {
        var document = Utf8GraphQLParser.Parse($$"""
            schema {{directive}} { query: Query }
            type Query { ping: String }
            enum fusion__Schema { A }
            """);
        using var settingsDocument = settings is null ? null : JsonDocument.Parse(settings);
        return CompositeSchemaBuilder.Create("default", document, settingsDocument);
    }
}

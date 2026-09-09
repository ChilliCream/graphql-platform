using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class OptionsTests
{
    [Fact]
    public void CostOptions_Should_HaveExpectedCostDefaults_When_Constructed()
    {
        // arrange & act
        var options = new CostOptions();

        // assert
        Assert.Equal(1_000, options.MaxFieldCost);
        Assert.Equal(1_000, options.MaxTypeCost);
        Assert.Equal(double.PositiveInfinity, options.DefaultListSize);
    }

    [Fact]
    public void CostOptions_Should_HaveExpectedEngineDefaults_When_Constructed()
    {
        // arrange & act
        var options = new CostOptions();

        // assert
        Assert.Equal(256, options.CostPlanCacheSize);
        Assert.Null(options.MaxResponseSize);
        Assert.Null(options.CaseBudget);
    }

    [Theory]
    [InlineData(null, 4096)]
    [InlineData(16, 16)]
    public async Task AddCostAnalyzer_Should_CreateImmutableSchemaSnapshot_When_CaseBudgetIsConfigured(
        int? caseBudget,
        int expectedCaseBudget)
    {
        // arrange
        var requestExecutor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType(d => d.Name("Query").Field("hello").Resolve("world"))
            .ModifyCostOptions(o => o.CaseBudget = caseBudget)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var snapshot = requestExecutor.Schema.Services.GetRequiredService<CostSchemaSnapshot>();
        var optionsCopy = snapshot.Options;
        optionsCopy.CaseBudget = -1;

        // assert
        Assert.Same(
            snapshot,
            requestExecutor.Schema.Services.GetRequiredService<CostSchemaSnapshot>());
        Assert.Equal(expectedCaseBudget, snapshot.Options.CaseBudget);
    }

    [Theory]
    [InlineData(typeof(FilterCostOptions), "VariableMultiplier")]
    [InlineData(typeof(SortCostOptions), "VariableMultiplier")]
    [InlineData(typeof(RequestCostOptions), "FilterVariableMultiplier")]
    public void VariableMultiplier_Should_BeObsoleteError_When_Reflected(Type declaringType, string memberName)
    {
        // arrange
        var property = declaringType.GetProperty(memberName);

        // act
        var obsolete = property?.GetCustomAttribute<ObsoleteAttribute>();

        // assert
        Assert.NotNull(obsolete);
        Assert.True(obsolete.IsError);
    }
}

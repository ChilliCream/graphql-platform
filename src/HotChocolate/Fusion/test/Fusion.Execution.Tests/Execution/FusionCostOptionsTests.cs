using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionCostOptionsTests : FusionTestBase
{
    [Fact]
    public void Defaults_Should_Match_Expected_Values_When_Constructed()
    {
        // arrange
        // act
        var options = new FusionCostOptions();

        // assert
        options.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 1000.0,
              "MaxTypeCost": 1000.0,
              "EnforceCostLimits": true,
              "SkipAnalyzer": false,
              "MaxResponseSize": null,
              "CostPlanCacheSize": 256,
              "CaseBudget": null,
              "CaseBudgetExceededBehavior": null
            }
            """);
    }

    [Fact]
    public async Task MaxFieldCost_Should_Throw_When_Set_After_Executor_Is_Built()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyCostOptions(o => o.MaxFieldCost = 500)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var costOptions = executor.Schema.Services.GetRequiredService<FusionCostOptions>();

        // assert
        Assert.Equal(500, costOptions.MaxFieldCost);
        Assert.Throws<InvalidOperationException>(() => costOptions.MaxFieldCost = 1);
    }
}

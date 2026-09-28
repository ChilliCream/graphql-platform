using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class CostOptionsConfigurationTests : FusionTestBase
{
    [Fact]
    public async Task Cost_Options_Should_Be_Configurable_Through_ModifyCostOptions()
    {
        // arrange
        const double maxFieldCost = 250;
        const bool enforceCostLimits = false;

        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyCostOptions(
                o =>
                {
                    o.MaxFieldCost = maxFieldCost;
                    o.EnforceCostLimits = enforceCostLimits;
                })
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));

        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var costOptions = executor.Schema.Services.GetRequiredService<FusionCostOptions>();

        // assert
        Assert.Equal(maxFieldCost, costOptions.MaxFieldCost);
        Assert.Equal(enforceCostLimits, costOptions.EnforceCostLimits);
    }
}

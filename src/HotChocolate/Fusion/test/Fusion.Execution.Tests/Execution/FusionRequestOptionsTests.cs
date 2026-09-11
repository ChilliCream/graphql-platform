using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestOptionsTests : FusionTestBase
{
    [Fact]
    public void Cost_Should_HaveExpectedDefaults_When_Constructed()
    {
        // arrange
        // act
        var options = new FusionRequestOptions();

        // assert
        options.Cost.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 1000.0,
              "MaxTypeCost": 1000.0,
              "EnforceCostLimits": true,
              "SkipAnalyzer": false,
              "MaxResponseSize": null,
              "DefaultListSize": "Infinity",
              "CostPlanCacheSize": 256,
              "CaseBudget": null
            }
            """);
    }

    [Fact]
    public void Clone_Should_DeepCopyCostOptions_When_Called()
    {
        // arrange
        var options = new FusionRequestOptions();
        options.Cost.MaxFieldCost = 500;
        options.Cost.MaxTypeCost = 750;
        options.Cost.EnforceCostLimits = false;
        options.Cost.MaxResponseSize = 2_000;
        options.Cost.DefaultListSize = 10;
        options.Cost.CostPlanCacheSize = 64;
        options.Cost.CaseBudget = 128;

        // act
        var clone = options.Clone();
        clone.Cost.MaxFieldCost = 999;

        // assert
        Assert.NotSame(options.Cost, clone.Cost);
        Assert.Equal(500, options.Cost.MaxFieldCost);
        clone.Cost.MatchInlineSnapshot(
            """
            {
              "MaxFieldCost": 999.0,
              "MaxTypeCost": 750.0,
              "EnforceCostLimits": false,
              "SkipAnalyzer": false,
              "MaxResponseSize": 2000.0,
              "DefaultListSize": 10.0,
              "CostPlanCacheSize": 64,
              "CaseBudget": 128
            }
            """);
    }

    [Fact]
    public void MakeReadOnly_Should_PropagateToCostOptions_When_Called()
    {
        // arrange
        var options = new FusionRequestOptions();

        // act
        options.MakeReadOnly();

        // assert
        Assert.Throws<InvalidOperationException>(() => options.Cost.MaxFieldCost = 1);
    }

    [Fact]
    public async Task ModifyCostOptions_Should_SetCostOptions_When_ConfiguredViaBuilder()
    {
        // arrange
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyCostOptions(o =>
            {
                o.MaxFieldCost = 250;
                o.EnforceCostLimits = false;
            })
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                        field: String!
                    }
                    """));

        // act
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var executor = await serviceProvider.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        var requestOptions = executor.Schema.Services.GetRequiredService<FusionRequestOptions>();
        Assert.Equal(250, requestOptions.Cost.MaxFieldCost);
        Assert.False(requestOptions.Cost.EnforceCostLimits);
    }
}

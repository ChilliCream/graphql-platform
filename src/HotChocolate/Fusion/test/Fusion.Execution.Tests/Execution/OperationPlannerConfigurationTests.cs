using HotChocolate.Execution;
using HotChocolate.Fusion.Planning;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class OperationPlannerConfigurationTests : FusionTestBase
{
    [Fact]
    public async Task Planner_Guardrails_Should_Be_Configurable_Through_ModifyPlannerOptions()
    {
        // arrange
        var maxPlanningTime = TimeSpan.FromSeconds(3);
        const int maxExpandedNodes = 1234;
        const int maxQueueSize = 4321;
        const int maxGeneratedOptionsPerWorkItem = 87;

        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyPlannerOptions(
                o =>
                {
                    o.MaxPlanningTime = maxPlanningTime;
                    o.MaxExpandedNodes = maxExpandedNodes;
                    o.MaxQueueSize = maxQueueSize;
                    o.MaxGeneratedOptionsPerWorkItem = maxGeneratedOptionsPerWorkItem;
                })
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));

        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

        // act
        var planner = executor.Schema.Services.GetRequiredService<OperationPlanner>();

        // assert
        Assert.Equal(maxPlanningTime, planner.Options.MaxPlanningTime);
        Assert.Equal(maxExpandedNodes, planner.Options.MaxExpandedNodes);
        Assert.Equal(maxQueueSize, planner.Options.MaxQueueSize);
        Assert.Equal(maxGeneratedOptionsPerWorkItem, planner.Options.MaxGeneratedOptionsPerWorkItem);
    }
}

using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerTests : FusionTestBase
{
    [Fact]
    public async Task CreateExecutor()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                schema @schemaName(value: "A") {
                    query: Query
                }

                type Query {
                    foo: String
                }
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .Services
                .BuildServiceProvider();

        // act
        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();

        // assert
        Assert.Equal(ISchemaDefinition.DefaultName, executor.Schema.Name);
    }

    [Fact]
    public async Task GetOperationPlanFromExecution()
    {
        // arrange
        var schemaDocument =
            ComposeSchemaDocument(
                """
                schema @schemaName(value: "A") {
                    query: Query
                }

                type Query {
                    foo: String
                }
                """);

        var services =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(schemaDocument)
                .UseDefaultPipeline()
                .InsertUseRequest(
                    before: nameof(OperationExecutionMiddleware),
                    (_, _) =>
                    {
                        return context =>
                        {
                            var plan = context.GetOperationPlan();
                            context.Result =
                                OperationResultBuilder.New()
                                    .SetData(
                                        new Dictionary<string, object?>
                                        {
                                            { "foo", null }
                                        })
                                    .SetContextData(
                                        new Dictionary<string, object?>
                                        {
                                            { "operationPlan", plan }
                                        })
                                        .Build();
                            return ValueTask.CompletedTask;
                        };
                    })
                .Services
                .BuildServiceProvider();

        var executorProvider = services.GetRequiredService<IRequestExecutorProvider>();
        var executor = await executorProvider.GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query Test {
                        foo
                    }
                    """)
                .Build());

        // assert
        Assert.NotNull(result.ContextData);
        Assert.True(result.ContextData.TryGetValue("operationPlan", out var operationPlan));
        Assert.NotNull(operationPlan);
        Assert.Equal("Test", Assert.IsType<OperationPlan>(operationPlan).OperationName);
    }
}

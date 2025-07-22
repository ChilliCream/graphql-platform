using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Pipeline;
using HotChocolate.Fusion.Logging;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorManagerTests
{
    [Fact]
    public async Task CreateExecutor()
    {
        // arrange
        var schemaDocument =
            ComposeSchema(
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
            ComposeSchema(
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
        Assert.Equal("Test", Assert.IsType<OperationExecutionPlan>(operationPlan).OperationName);
    }

    protected static DocumentNode ComposeSchema(
        [StringSyntax("graphql")] params string[] schemas)
    {
        var compositionLog = new CompositionLog();
        var composer = new SchemaComposer(schemas, compositionLog);
        var result = composer.Compose();

        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(result.Errors[0].Message);
        }

        return result.Value.ToSyntaxNode();
    }
}

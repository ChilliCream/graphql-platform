using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public class FusionRequestExecutorProxyTests : FusionTestBase
{
    [Fact]
    public async Task Ensure_Executor_Is_Cached()
    {
        // arrange
        var manager =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddInMemoryConfiguration(
                    ComposeSchemaDocument(
                        """
                        type Query {
                          field: String!
                        }
                        """))
                .Services
                .BuildServiceProvider()
                .GetRequiredService<FusionRequestExecutorManager>();

        // act
        var proxy = new RequestExecutorProxy(manager, manager, ISchemaDefinition.DefaultName);
        var a = await proxy.GetExecutorAsync(CancellationToken.None);
        var b = await proxy.GetExecutorAsync(CancellationToken.None);

        // assert
        Assert.Same(a, b);
    }

    [Fact]
    public async Task Ensure_Executor_Is_Correctly_Swapped_When_Evicted()
    {
        // arrange
        var executorUpdatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var configProvider = new TestFusionConfigurationProvider(
            CreateFusionConfiguration(
                """
                type Query {
                  field: String!
                }
                """));

        var manager =
            new ServiceCollection()
                .AddGraphQLGateway()
                .AddConfigurationProvider(_ => configProvider)
                .Services
                .BuildServiceProvider()
                .GetRequiredService<FusionRequestExecutorManager>();

        var proxy = new TestProxy(manager, manager, ISchemaDefinition.DefaultName);
        proxy.ExecutorUpdated += () => executorUpdatedResetEvent.Set();

        // act
        var a = await proxy.GetExecutorAsync(cts.Token);

        configProvider.UpdateConfiguration(
            CreateFusionConfiguration(
                """
                type Query {
                  field2: String!
                }
                """));

        executorUpdatedResetEvent.Wait(cts.Token);
        var b = await proxy.GetExecutorAsync(cts.Token);

        // assert
        Assert.NotSame(a, b);
    }

    private class TestProxy(
        IRequestExecutorProvider executorProvider,
        IRequestExecutorEvents executorEvents,
        string schemaName)
        : RequestExecutorProxy(executorProvider, executorEvents, schemaName)
    {
        public event Action? ExecutorUpdated;

        protected override void OnAfterRequestExecutorSwapped(
            IRequestExecutor newExecutor,
            IRequestExecutor oldExecutor)
        {
            ExecutorUpdated?.Invoke();
        }
    }
}

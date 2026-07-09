using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Types.Mutable.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion;

public sealed class FusionExecutorWarmupTests : FusionTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Ensure_Executor_Is_Created_During_Startup(bool lazyInitialization)
    {
        // arrange
        var executorCreated = false;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var createdResetEvent = new ManualResetEventSlim(false);

        var schema = SchemaParser.Parse(
            """
            type Query @fusion__type(schema: A) {
              field: String! @fusion__field(schema: A)
            }

            enum fusion__Schema {
              A
            }
            """).ToSyntaxNode();

        var services = new ServiceCollection();
        services
            .AddGraphQLGatewayServer()
            .AddInMemoryConfiguration(schema)
            .ModifyOptions(o => o.LazyInitialization = lazyInitialization);
        var provider = services.BuildServiceProvider();

        var executorManager = provider.GetRequiredService<FusionRequestExecutorManager>();
        var warmupService = provider.GetRequiredService<IHostedService>();

        executorManager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Created)
            {
                executorCreated = true;
                createdResetEvent.Set();
            }
        }));

        // act
        await warmupService.StartAsync(cts.Token);

        // assert
        if (lazyInitialization)
        {
            Assert.False(executorCreated);
        }
        else
        {
            createdResetEvent.Wait(cts.Token);
            Assert.True(executorCreated);
        }
    }
}

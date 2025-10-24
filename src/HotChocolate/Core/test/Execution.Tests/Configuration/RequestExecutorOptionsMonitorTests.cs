using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration;

public class RequestExecutorOptionsMonitorTests
{
    [Fact]
    public async Task RequestExecutorSetup_Can_Be_Provided_Through_RequestExecutorOptionsMonitor()
    {
        // arrange
        var monitor = new TestOptionsMonitor();
        monitor.Set(ISchemaDefinition.DefaultName, new RequestExecutorSetup
        {
            SchemaBuilder = SchemaBuilder.New()
                .AddQueryType(d => d.Field("field").Resolve("")),
            Pipeline = { new RequestMiddlewareConfiguration((_, _) => _ => ValueTask.CompletedTask) }
        });

        var services = new ServiceCollection();
        services.AddSingleton<IRequestExecutorOptionsMonitor>(_ => monitor);

        services.AddGraphQLServer();

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync();

        // assert
        executor.Schema.MatchInlineSnapshot(
            """
            schema {
              query: ObjectType
            }

            type ObjectType {
              field: String
            }
            """);
    }

    [Fact]
    public async Task RequestExecutor_Can_Be_Reloaded_Through_RequestExecutorOptionsMonitor()
    {
        // arrange
        var executorEvictedResetEvent = new ManualResetEventSlim(false);
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        var monitor = new TestOptionsMonitor();
        monitor.Set(ISchemaDefinition.DefaultName, new RequestExecutorSetup
        {
            SchemaBuilder = SchemaBuilder.New()
                .AddQueryType(d => d.Field("field").Resolve("")),
            Pipeline = { new RequestMiddlewareConfiguration((_, _) => _ => ValueTask.CompletedTask) }
        });

        var services = new ServiceCollection();
        services.AddSingleton<IRequestExecutorOptionsMonitor>(_ => monitor);

        services.AddGraphQLServer();

        var manager = services.BuildServiceProvider().GetRequiredService<RequestExecutorManager>();

        manager.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Evicted)
            {
                executorEvictedResetEvent.Set();
            }
        }));

        // act
        // assert
        var initialExecutor = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        monitor.Set(ISchemaDefinition.DefaultName, new RequestExecutorSetup
        {
            SchemaBuilder = SchemaBuilder.New()
                .AddQueryType(d => d.Field("field2").Resolve("")),
            Pipeline = { new RequestMiddlewareConfiguration((_, _) => _ => ValueTask.CompletedTask) }
        });

        executorEvictedResetEvent.Wait(cts.Token);

        var executorAfterEviction = await manager.GetExecutorAsync(cancellationToken: cts.Token);

        Assert.NotSame(initialExecutor, executorAfterEviction);

        executorAfterEviction.Schema.MatchInlineSnapshot(
            """
            schema {
              query: ObjectType
            }

            type ObjectType {
              field2: String
            }
            """);

        cts.Dispose();
    }

    private sealed class TestOptionsMonitor : IRequestExecutorOptionsMonitor
    {
        private readonly Dictionary<string, RequestExecutorSetup> _setups = new();
        private readonly List<Action<string>> _listeners = new();

        public void Set(string schemaName, RequestExecutorSetup setup)
        {
            _setups[schemaName] = setup;

            foreach (var listener in _listeners)
            {
                listener(schemaName);
            }
        }

        public RequestExecutorSetup Get(string schemaName)
        {
            return _setups[schemaName];
        }

        public IDisposable OnChange(Action<string> listener)
        {
            _listeners.Add(listener);
            return new Unsubscriber(this, listener);
        }

        private void Unsubscribe(Action<string> listener)
        {
            _listeners.Remove(listener);
        }

        private sealed class Unsubscriber(
            TestOptionsMonitor monitor,
            Action<string> listener)
            : IDisposable
        {
            public void Dispose() => monitor.Unsubscribe(listener);
        }
    }
}

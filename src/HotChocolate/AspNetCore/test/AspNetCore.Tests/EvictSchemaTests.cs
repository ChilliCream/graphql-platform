using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class EvictSchemaTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Evict_Default_Schema()
    {
        // arrange
        var newExecutorCreatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var server = CreateStarWarsServer();

        var time1 = await server.GetAsync(new ClientQueryRequest { Query = "{ time }" });

        var events = server.Services.GetRequiredService<IRequestExecutorEvents>();
        events.Subscribe(
            new RequestExecutorEventObserver(
                @event =>
                {
                    if (@event.Type == RequestExecutorEventType.Created)
                    {
                        newExecutorCreatedResetEvent.Set();
                    }
                }));

        // act
        await server.GetAsync(new ClientQueryRequest { Query = "{ evict }" });
        newExecutorCreatedResetEvent.Wait(cts.Token);

        // assert
        var time2 = await server.GetAsync(new ClientQueryRequest { Query = "{ time }" });
        Assert.False(((long)time1.Data!["time"]!).Equals((long)time2.Data!["time"]!));
    }

    [Fact]
    public async Task Evict_Named_Schema()
    {
        // arrange
        var newExecutorCreatedResetEvent = new ManualResetEventSlim(false);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var server = CreateStarWarsServer();

        var time1 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }" },
            "/evict");

        var events = server.Services.GetRequiredService<IRequestExecutorEvents>();
        events.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Created)
            {
                newExecutorCreatedResetEvent.Set();
            }
        }));

        // act
        await server.GetAsync(
            new ClientQueryRequest { Query = "{ evict }" },
            "/evict");
        newExecutorCreatedResetEvent.Wait(cts.Token);

        // assert
        var time2 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }" },
            "/evict");
        Assert.False(((long)time1.Data!["time"]!).Equals((long)time2.Data!["time"]!));
    }
}

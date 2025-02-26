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
        var newExecutorCreatedResetEvent = new AutoResetEvent(false);
        var server = CreateStarWarsServer();

        var time1 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }", });

        var resolver = server.Services.GetRequiredService<IRequestExecutorResolver>();
        resolver.Events.Subscribe(new RequestExecutorEventObserver(@event =>
        {
            if (@event.Type == RequestExecutorEventType.Created)
            {
                newExecutorCreatedResetEvent.Set();
            }
        }));

        // act
        await server.GetAsync(
            new ClientQueryRequest { Query = "{ evict }", });
        newExecutorCreatedResetEvent.WaitOne();

        // assert
        var time2 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }", });
        Assert.False(((long)time1.Data!["time"]!).Equals((long)time2.Data!["time"]!));
    }

    [Fact]
    public async Task Evict_Named_Schema()
    {
        // arrange
        var server = CreateStarWarsServer();

        var time1 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }", },
            "/evict");

        // act
        await server.GetAsync(
            new ClientQueryRequest { Query = "{ evict }", },
            "/evict");

        // assert
        var time2 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }", },
            "/evict");
        Assert.False(((long)time1.Data!["time"]!).Equals((long)time2.Data!["time"]!));
    }
}

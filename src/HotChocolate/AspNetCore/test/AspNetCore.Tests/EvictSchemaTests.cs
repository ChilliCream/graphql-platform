using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class EvictSchemaTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Evict_Default_Schema()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var server = CreateStarWarsServer();

        var time1 = (long)(await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }" })).Data!["time"]!;

        // act
        await server.GetAsync(new ClientQueryRequest { Query = "{ evict }" });

        // assert
        // Eviction re-creates and swaps in a new executor asynchronously, and there is no event
        // for "the endpoint now serves the new schema", so poll until the reported schema
        // creation time changes.
        long time2;
        do
        {
            await Task.Delay(25, cts.Token);
            time2 = (long)(await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" })).Data!["time"]!;
        }
        while (time2 == time1);

        Assert.NotEqual(time1, time2);
    }

    [Fact]
    public async Task Evict_Named_Schema()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var server = CreateStarWarsServer();

        var time1 = (long)(await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }" },
            "/evict")).Data!["time"]!;

        // act
        await server.GetAsync(
            new ClientQueryRequest { Query = "{ evict }" },
            "/evict");

        // assert
        long time2;
        do
        {
            await Task.Delay(25, cts.Token);
            time2 = (long)(await server.GetAsync(
                new ClientQueryRequest { Query = "{ time }" },
                "/evict")).Data!["time"]!;
        }
        while (time2 == time1);

        Assert.NotEqual(time1, time2);
    }
}

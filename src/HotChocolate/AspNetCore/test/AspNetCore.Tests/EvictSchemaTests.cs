using HotChocolate.AspNetCore.Tests.Utilities;

namespace HotChocolate.AspNetCore;

public class EvictSchemaTests : ServerTestBase
{
    public EvictSchemaTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Evict_Default_Schema()
    {
        // arrange
        var server = CreateStarWarsServer();

        var time1 = await server.GetAsync(
            new ClientQueryRequest { Query = "{ time }", });

        // act
        await server.GetAsync(
            new ClientQueryRequest { Query = "{ evict }", });

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

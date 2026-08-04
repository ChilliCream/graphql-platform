using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Fusion.Transport.Http;

public static class GraphQLServerHelper
{
    public static async Task<(TestServer, WebApplication)> CreateTestServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder
            .AddGraphQL()
            .AddQueryType<Query>();

        var app = builder.Build();
        app.MapGraphQL();

        await app.StartAsync();
        return (app.GetTestServer(), app);
    }
}

public class Query
{
    private readonly string[] _items;

    public Query()
    {
        _items = Enumerable.Range(0, 500_000).Select(i => $"Item {i}").ToArray();
    }

    public string[] Items => _items;
}

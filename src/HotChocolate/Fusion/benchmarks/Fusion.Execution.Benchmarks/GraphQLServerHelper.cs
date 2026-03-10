using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fusion.Execution.Benchmarks;

public static class GraphQLServerHelper
{
    public static async Task<(TestServer, WebApplication)> CreateTestServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

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
    private readonly string[] _fewItems;

    public Query()
    {
        _items = Enumerable.Range(0, 500_000).Select(i => $"Item {i}").ToArray();
        _fewItems = _items.Take(10).ToArray();
    }

    public string[] Items => _items;

    public string[] FewItems => _fewItems;
}

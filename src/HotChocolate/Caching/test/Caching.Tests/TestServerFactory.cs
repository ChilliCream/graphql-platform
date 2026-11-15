using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Caching.Http.Tests;

public class TestServerFactory : IAsyncDisposable
{
    private readonly List<WebApplication> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        configureServices(builder.Services);

        var app = builder.Build();
        configureApplication(app);

        app.Start();
        _instances.Add(app);
        return app.GetTestServer();
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var app in _instances)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public class TestServerFactory : IAsyncDisposable
{
    private readonly List<WebApplication> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseTestServer();
        builder.Services.AddHttpContextAccessor();
        configureServices(builder.Services);

        var app = builder.Build();
        configureApplication(app);

        app.Start();
        _instances.Add(app);
        return app.GetTestServer();
    }

    public ValueTask DisposeAsync() => DisposeServersAsync();

    /// <summary>
    /// Stops, disposes, and releases every server this factory has created so far.
    /// </summary>
    public async ValueTask DisposeServersAsync()
    {
        foreach (var app in _instances)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }

        _instances.Clear();
    }
}

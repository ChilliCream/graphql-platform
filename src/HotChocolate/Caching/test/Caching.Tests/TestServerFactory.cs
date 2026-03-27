using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Caching.Http.Tests;

public class TestServerFactory : IDisposable
{
    private readonly List<WebApplication> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        configureServices(builder.Services);

        var app = builder.Build();
        configureApplication(app);

        app.Start();
        _instances.Add(app);
        return app.GetTestServer();
    }

    public void Dispose()
    {
        foreach (var app in _instances)
        {
            ((IHost)app).Dispose();
        }
    }
}

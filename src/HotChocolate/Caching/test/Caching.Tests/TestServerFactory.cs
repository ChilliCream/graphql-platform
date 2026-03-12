using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Caching.Http.Tests;

public class TestServerFactory : IDisposable
{
    private readonly List<IHost> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .Configure(configureApplication)
                    .ConfigureServices(configureServices);
            })
            .Start();

        _instances.Add(host);
        return host.GetTestServer();
    }

    public void Dispose()
    {
        foreach (var host in _instances)
        {
            host.Dispose();
        }
    }
}

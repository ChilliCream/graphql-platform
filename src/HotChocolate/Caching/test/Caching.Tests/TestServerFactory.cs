using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HotChocolate.Caching.Http.Tests;

public class TestServerFactory : IDisposable
{
    private readonly List<TestServer> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost
                    .ConfigureServices(configureServices)
                    .Configure(configureApplication)
                    .UseTestServer();
            })
            .Build();

        host.Start();
        var server = host.GetTestServer();
        _instances.Add(server);
        return server;
    }

    public void Dispose()
    {
        foreach (var testServer in _instances)
        {
            testServer.Dispose();
        }
    }
}

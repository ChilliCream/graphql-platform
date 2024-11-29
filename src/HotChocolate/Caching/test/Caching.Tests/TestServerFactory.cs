using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Caching.Http.Tests;

public class TestServerFactory : IDisposable
{
    private readonly List<TestServer> _instances = [];

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = new WebHostBuilder()
            .Configure(configureApplication)
            .ConfigureServices(services =>
            {
                configureServices?.Invoke(services);
            });

        var server = new TestServer(builder);
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

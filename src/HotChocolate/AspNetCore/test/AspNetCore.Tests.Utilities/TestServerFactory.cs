using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public class TestServerFactory : IDisposable
{
    private readonly List<TestServer> _instances = new();

    public TestServer Create(
        Action<IServiceCollection> configureServices,
        Action<IApplicationBuilder> configureApplication)
    {
        var builder = new WebHostBuilder()
            .Configure(configureApplication)
            .ConfigureServices(services =>
            {
                services.AddHttpContextAccessor();
                configureServices?.Invoke(services);
            });

        var server = new TestServer(builder);
        _instances.Add(server);
        return server;
    }

    public void Dispose()
    {
        var exceptions = new List<Exception>();
        foreach (var testServer in _instances)
        {
            try
            {
                testServer.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            throw new AggregateException(exceptions);
        }
    }
}

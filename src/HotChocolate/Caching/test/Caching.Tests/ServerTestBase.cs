using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Caching.Http.Tests;

public class ServerTestBase : IClassFixture<TestServerFactory>
{
    public ServerTestBase(TestServerFactory serverFactory)
    {
        ServerFactory = serverFactory;
    }

    protected TestServerFactory ServerFactory { get; }

    protected virtual TestServer CreateServer(Action<IServiceCollection>? configureServices = null)
    {
        return ServerFactory.Create(
            services =>
            {
                services.AddRouting();

                configureServices?.Invoke(services);
            },
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));
    }
}

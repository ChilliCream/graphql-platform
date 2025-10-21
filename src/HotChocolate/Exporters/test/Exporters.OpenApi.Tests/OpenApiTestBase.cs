using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Exporters.OpenApi;

public abstract class OpenApiTestBase
{
    protected static TestServer CreateBasicTestServer(IOpenApiDocumentStorage storage)
    {
        return CreateTestServer(
            configureRequestExecutor: b => b.AddOpenApiDocumentStorage(storage),
            configureOpenApi: o => o.AddGraphQL(),
            configureEndpoints: e => e.MapGraphQLEndpoints());
    }

    protected static TestServer CreateTestServer(
        Action<IRequestExecutorBuilder>? configureRequestExecutor = null,
        Action<OpenApiOptions>? configureOpenApi = null,
        Action<IEndpointRouteBuilder>? configureEndpoints = null)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(
                services =>
                {
                    services
                        .AddLogging()
                        .AddRouting();

                    services.AddOpenApi(options => configureOpenApi?.Invoke(options));

                    var executor = services
                            .AddGraphQL();

                    configureRequestExecutor?.Invoke(executor);
                })
            .Configure(
                app => app
                    .UseRouting()
                    .UseAuthentication()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapOpenApi();

                        configureEndpoints?.Invoke(endpoints);
                    }));

        return new TestServer(builder);
    }
}

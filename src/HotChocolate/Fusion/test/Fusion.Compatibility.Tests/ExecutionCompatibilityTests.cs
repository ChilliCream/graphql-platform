using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Language;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Compatibility;

/// <summary>
/// Proves, from a non-friend consumer, that a router configured entirely through the public
/// <see cref="IFusionRouterBuilder"/> surface executes a real query against a real source
/// schema and returns it through the public <see cref="IRequestExecutorProvider"/> entry point.
/// The source schema and the router each live in their own service provider, connected only
/// through an <see cref="HttpClient"/> whose primary handler is the source schema's
/// <see cref="TestServer"/> handler, the same way two separate processes would be connected.
/// </summary>
public sealed class ExecutionCompatibilityTests
{
    private static readonly DocumentNode s_composedDocument = Utf8GraphQLParser.Parse(
        """
        type Query @fusion__type(schema: A) {
          field: String @fusion__field(schema: A)
        }
        enum fusion__Schema { A }
        """);

    [Fact]
    public async Task GetExecutorAsync_Should_ExecuteQueryAgainstSourceSchema_When_RouterUsesOnlyPublicApis()
    {
        // arrange
        using var sourceServer = CreateSourceSchemaServer();

        var routerServices = new ServiceCollection();
        // The (name, baseAddress) overload of AddHttpClientConfiguration uses the source schema
        // name itself as the named HttpClient, so the primary handler is registered under "A".
        routerServices
            .AddHttpClient("A")
            .ConfigurePrimaryHttpMessageHandler(() => sourceServer.CreateHandler());

        routerServices
            .AddGraphQLRouter("exec-schema")
            .AddInMemoryConfiguration(s_composedDocument)
            .AddHttpClientConfiguration("A", new Uri(sourceServer.BaseAddress, "graphql"));

        // act
        await using var routerProvider = routerServices.BuildServiceProvider();
        var executor = await routerProvider
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync("exec-schema", TestContext.Current.CancellationToken);

        await using var result = await executor.ExecuteAsync(
            "{ field }", TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "field": "compat-value"
              }
            }
            """);
    }

    private static TestServer CreateSourceSchemaServer()
    {
        var hostBuilder = new WebHostBuilder()
            .ConfigureServices(services => services
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<SourceSchemaQuery>())
            .Configure(app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

        return new TestServer(hostBuilder);
    }

    public sealed class SourceSchemaQuery
    {
        public string Field => "compat-value";
    }
}

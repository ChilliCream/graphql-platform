using HotChocolate.AspNetCore.Extensions;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.AspNetCore.Tests.Utilities.Logging;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public abstract class ServerTestBase : IClassFixture<TestServerFactory>
{
    private readonly ITestOutputHelper? _testOutputHelper;

    protected ServerTestBase(TestServerFactory serverFactory, ITestOutputHelper? testOutputHelper = default)
    {
        ServerFactory = serverFactory;

        _testOutputHelper = testOutputHelper;
    }

    protected TestServerFactory ServerFactory { get; }

    protected virtual TestServer CreateStarWarsServer(
        string pattern = "/graphql",
        Action<IServiceCollection>? configureServices = default,
        Action<GraphQLEndpointConventionBuilder>? configureConventions = default)
    {
        return ServerFactory.Create(
            services =>
            {
                services
                    .AddTestLogging(_testOutputHelper)
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddTypeExtension<QueryExtension>()
                    .AddTypeExtension<SubscriptionsExtensions>()
                    .AddExportDirectiveType()
                    .AddStarWarsRepositories()
                    .AddInMemorySubscriptions()
                    .UseAutomaticPersistedQueryPipeline()
                    .ConfigureSchemaServices(s => s
                        .AddSingleton<PersistedQueryCache>()
                        .AddSingleton<IReadStoredQueries>(
                            c => c.GetRequiredService<PersistedQueryCache>())
                        .AddSingleton<IWriteStoredQueries>(
                            c => c.GetRequiredService<PersistedQueryCache>()))
                    .AddGraphQLServer("evict")
                    .AddQueryType(d => d.Name("Query"))
                    .AddTypeExtension<QueryExtension>()
                    .AddGraphQLServer("arguments")
                    .AddQueryType(d =>
                    {
                        d
                            .Name("QueryRoot");

                        d
                            .Field("double_arg")
                            .Argument("d", t => t.Type<FloatType>())
                            .Type<FloatType>()
                            .Resolve(c => c.ArgumentValue<double?>("d"));

                        d
                            .Field("decimal_arg")
                            .Argument("d", t => t.Type<DecimalType>())
                            .Type<DecimalType>()
                            .Resolve(c => c.ArgumentValue<decimal?>("d"));
                    })
                    .AddGraphQLServer("upload")
                    .AddQueryType<UploadQuery>();

                configureServices?.Invoke(services);
            },
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    var builder = endpoints.MapGraphQL(pattern);

                    configureConventions?.Invoke(builder);
                    endpoints.MapGraphQL("/evict", "evict");
                    endpoints.MapGraphQL("/arguments", "arguments");
                    endpoints.MapGraphQL("/upload", "upload");
                }));
    }

    protected virtual TestServer CreateServer(
        Action<IEndpointRouteBuilder>? configureConventions = default)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddTypeExtension<QueryExtension>()
                .AddTypeExtension<SubscriptionsExtensions>()
                .AddExportDirectiveType()
                .AddStarWarsRepositories(),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => configureConventions?.Invoke(endpoints)));
    }
}

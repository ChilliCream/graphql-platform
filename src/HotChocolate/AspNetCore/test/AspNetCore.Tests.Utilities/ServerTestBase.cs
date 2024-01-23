using HotChocolate.AspNetCore.Extensions;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace HotChocolate.AspNetCore.Tests.Utilities;

public abstract class ServerTestBase : IClassFixture<TestServerFactory>
{
    protected ServerTestBase(TestServerFactory serverFactory)
    {
        ServerFactory = serverFactory;
    }

    protected TestServerFactory ServerFactory { get; }

    protected virtual TestServer CreateStarWarsServer(
        string pattern = "/graphql",
        Action<IServiceCollection>? configureServices = default,
        Action<GraphQLEndpointConventionBuilder>? configureConventions = default,
        ITestOutputHelper? output = null)
    {
        return ServerFactory.Create(
            services =>
            {
                services
                    .AddRouting()
                    .AddHttpResponseFormatter()
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddTypeExtension<QueryExtension>()
                    .AddTypeExtension<SubscriptionsExtensions>()
                    .AddExportDirectiveType()
                    .AddStarWarsRepositories()
                    .AddInMemorySubscriptions()
                    .UseAutomaticPersistedQueryPipeline()
                    .ConfigureSchemaServices(
                        s => s
                            .AddSingleton<PersistedQueryCache>()
                            .AddSingleton<IReadStoredQueries>(
                                c => c.GetRequiredService<PersistedQueryCache>())
                            .AddSingleton<IWriteStoredQueries>(
                                c => c.GetRequiredService<PersistedQueryCache>()))
                    .ModifyOptions(
                        o =>
                        {
                            o.EnableDefer = true;
                            o.EnableStream = true;
                        })
                    .AddGraphQLServer("StarWars")
                    .AddStarWarsTypes()
                    .AddGraphQLServer("evict")
                    .AddQueryType(d => d.Name("Query"))
                    .AddTypeExtension<QueryExtension>()
                    .AddGraphQLServer("arguments")
                    .AddQueryType(
                        d =>
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

                if (output is not null)
                {
                    services
                        .AddGraphQL()
                        .AddDiagnosticEventListener(_ => new SubscriptionTestDiagnostics(output));
                }

                configureServices?.Invoke(services);
            },
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(
                    endpoints =>
                    {
                        var builder = endpoints.MapGraphQL(pattern)
                            .WithOptions(new GraphQLServerOptions
                            {
                                EnableBatching = true,
                                AllowedGetOperations = AllowedGetOperations.Query | AllowedGetOperations.Subscription,
                            });

                        configureConventions?.Invoke(builder);
                        endpoints.MapGraphQL("/evict", "evict");
                        endpoints.MapGraphQL("/arguments", "arguments");
                        endpoints.MapGraphQL("/upload", "upload");
                        endpoints.MapGraphQL("/starwars", "StarWars");
                        endpoints.MapGraphQL("/test", "test");
                        endpoints.MapGraphQL("/batching").
                            WithOptions(new GraphQLServerOptions
                            {
                                // with defaults
                                // EnableBatching = false
                            });
                    }));
    }

    protected virtual TestServer CreateServer(
        Action<IEndpointRouteBuilder>? configureConventions = default)
    {
        return ServerFactory.Create(
            services => services
                .AddRouting()
                .AddHttpResponseFormatter()
                .AddGraphQLServer()
                .AddStarWarsTypes()
                .AddTypeExtension<QueryExtension>()
                .AddTypeExtension<SubscriptionsExtensions>()
                .AddExportDirectiveType()
                .AddStarWarsRepositories()
                .ModifyOptions(
                    o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    }),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => configureConventions?.Invoke(endpoints)));
    }
}

using System;
using HotChocolate.AspNetCore.Extensions;
using HotChocolate.AspNetCore.Serialization;
using HotChocolate.Execution;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.AspNetCore.Utilities;

public class ServerTestBase : IClassFixture<TestServerFactory>
{
    public ServerTestBase(TestServerFactory serverFactory)
    {
        ServerFactory = serverFactory;
    }

    protected TestServerFactory ServerFactory { get; }

    protected virtual TestServer CreateStarWarsServer(
        string pattern = "/graphql",
        Action<IServiceCollection> configureServices = default,
        Action<GraphQLEndpointConventionBuilder> configureConventions = default)
    {
        return ServerFactory.Create(
            services =>
            {
                TestSocketSessionInterceptor testInterceptor = new();

                services.AddSingleton(testInterceptor);

                services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddTypeExtension<QueryExtension>()
                    .AddTypeExtension<SubscriptionsExtensions>()
                    .AddExportDirectiveType()
                    .AddSocketSessionInterceptor(x => testInterceptor)
                    .AddStarWarsRepositories()
                    .AddInMemorySubscriptions()
                    .UseAutomaticPersistedQueryPipeline()
                    .ConfigureSchemaServices(services =>
                        services
                            .AddSingleton<PersistedQueryCache>()
                            .AddSingleton<IReadStoredQueries>(
                                c => c.GetService<PersistedQueryCache>())
                            .AddSingleton<IWriteStoredQueries>(
                                c => c.GetService<PersistedQueryCache>()))
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
                    GraphQLEndpointConventionBuilder builder = endpoints.MapGraphQL(pattern);

                    configureConventions?.Invoke(builder);
                    endpoints.MapGraphQL("/evict", "evict");
                    endpoints.MapGraphQL("/arguments", "arguments");
                    endpoints.MapGraphQL("/upload", "upload");
                }));
    }

    protected virtual TestServer CreateServer(
        Action<IEndpointRouteBuilder> configureConventions = default)
    {
        return ServerFactory.Create(
            services =>
            {
                TestSocketSessionInterceptor testInterceptor = new();

                services.AddSingleton(testInterceptor);

                services
                    .AddRouting()
                    .AddHttpResultSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                    .AddStarWarsTypes()
                    .AddTypeExtension<QueryExtension>()
                    .AddTypeExtension<SubscriptionsExtensions>()
                    .AddExportDirectiveType()
                    .AddSocketSessionInterceptor(_ => testInterceptor)
                    .AddStarWarsRepositories();
            },
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => configureConventions?.Invoke(endpoints)));
    }
}

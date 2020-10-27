using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Xunit;
using System;

namespace HotChocolate.AspNetCore.Utilities
{
    public class ServerTestBase : IClassFixture<TestServerFactory>
    {
        public ServerTestBase(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; }

        protected virtual TestServer CreateStarWarsServer(
            string pattern = "/graphql",
            Action<IGraphQLEndpointConventionBuilder> configureConventions = default)
        {
            return ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddHttpRequestSerializer(HttpResultSerialization.JsonArray)
                    .AddGraphQLServer()
                        .AddStarWarsTypes()
                        .AddTypeExtension<QueryExtension>()
                        .AddExportDirectiveType()
                        .AddStarWarsRepositories()
                        .AddInMemorySubscriptions()
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
                        }),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints =>
                    {
                        IGraphQLEndpointConventionBuilder builder = endpoints.MapGraphQL(pattern);

                        if (configureConventions is { })
                        {
                            configureConventions(builder);
                        }

                        endpoints.MapGraphQL("/evict", "evict");
                        endpoints.MapGraphQL("/arguments", "arguments");
                    }));
        }
    }
}

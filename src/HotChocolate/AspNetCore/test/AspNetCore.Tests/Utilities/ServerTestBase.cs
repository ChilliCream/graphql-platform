using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars;
using Xunit;

namespace HotChocolate.AspNetCore.Utilities
{
    public class ServerTestBase : IClassFixture<TestServerFactory>
    {
        public ServerTestBase(TestServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        protected TestServerFactory ServerFactory { get; set; }

        protected TestServer CreateStarWarsServer(string pattern = "/graphql") =>
            ServerFactory.Create(
                services => services
                    .AddRouting()
                    .AddGraphQLServer()
                        .AddStarWarsTypes()
                        .AddTypeExtension<QueryExtension>()
                        .AddExportDirectiveType()
                        .AddStarWarsRepositories()
                        .AddInMemorySubscriptions()
                    .AddGraphQLServer("evict")
                        .AddQueryType(d => d.Name("Query"))
                        .AddTypeExtension<QueryExtension>(),
                app => app
                    .UseWebSockets()
                    .UseRouting()
                    .UseEndpoints(endpoints => 
                    {
                        endpoints.MapGraphQL(pattern);
                        endpoints.MapGraphQL("evict", "evict");
                    }));
    }
}

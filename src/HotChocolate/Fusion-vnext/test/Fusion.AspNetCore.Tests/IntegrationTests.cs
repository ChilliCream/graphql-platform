using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class IntegrationTests : FusionTestBase
{
    [Fact]
    public async Task Foo()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType(
                d => d.Name("Query")
                    .Field("foo")
                    .Resolve("foo")));

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType(
                d => d.Name("Query")
                    .Field("bar")
                    .Resolve("bar")));

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            """
            {
              foo
              bar
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }

    [Fact]
    public async Task Bar()
    {
        var server = _testServerSession.CreateServer(
            services =>
            {
                services.AddRouting();

                services
                    .AddHttpClient("SUBGRAPH1", c => c.BaseAddress = new Uri("http://localhost:5095/graphql"));

                services
                    .AddHttpClient("SUBGRAPH2", c => c.BaseAddress = new Uri("http://localhost:5096/graphql"));

                services
                    .AddGraphQLGatewayServer()
                    .AddFileSystemConfiguration("/Users/michael/local/play/FusionServer/Gateway/gateway.graphql")
                    .AddHttpClientConfiguration("SUBGRAPH1", new Uri("http://localhost:5095/graphql"))
                    .AddHttpClientConfiguration("SUBGRAPH2", new Uri("http://localhost:5096/graphql"));
            },
            app =>
            {
                app.UseWebSockets();
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            });

        using var client = GraphQLHttpClient.Create(server.CreateClient());

        using var result = await client.PostAsync(
            """
            {
                book {
                  title
                }
            }
            """,
            new Uri("http://localhost:5000/graphql"));

        // act
        using var response = await result.ReadAsResultAsync();
        response.MatchSnapshot();
    }
}

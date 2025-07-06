using System.Text;
using HotChocolate.Buffers;
using HotChocolate.Types;
using HotChocolate.Execution;
using HotChocolate.Transport.Formatters;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
                    .Resolve("foo")) );

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
}

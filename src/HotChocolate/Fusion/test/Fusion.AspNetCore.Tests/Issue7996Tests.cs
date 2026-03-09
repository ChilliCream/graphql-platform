using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class Issue7996Tests : FusionTestBase
{
    [Fact]
    public async Task Mutation_Returned_Viewer_Can_Resolve_Field_From_Another_Subgraph()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            b => b
                .AddQueryType<SourceSchemaA.Query>()
                .AddMutationType<SourceSchemaA.Mutation>());

        using var serverB = CreateSourceSchema(
            "B",
            b => b.AddQueryType<SourceSchemaB.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new HotChocolate.Transport.OperationRequest(
            """
            mutation {
              doSomething {
                something
                viewer {
                  subgraphA
                  subgraphB
                }
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    private static class SourceSchemaA
    {
        public class Query
        {
            [Shareable]
            public Viewer Viewer => new("subgraphA");
        }

        public class Mutation
        {
            public DoSomethingPayload DoSomething() => new(123, new Viewer("subgraphA"));
        }

        public sealed record DoSomethingPayload(int Something, Viewer Viewer);

        public sealed record Viewer(string SubgraphA);
    }

    private static class SourceSchemaB
    {
        public class Query
        {
            [Shareable]
            public Viewer Viewer => new("subgraphB");
        }

        public sealed record Viewer(string SubgraphB);
    }
}

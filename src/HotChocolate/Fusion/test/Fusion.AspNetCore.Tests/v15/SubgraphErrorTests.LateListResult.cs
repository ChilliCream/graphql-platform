using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsAnAlreadyNulledList(ErrorHandlingMode onError)
    {
        // arrange
        var order = new SourceResultOrder("AB");
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { rows: [Row!] @shareable }
            type Row @shareable { name: String! }
            """,
            """{ "data": { "rows": [{ "name": null }] } }""",
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { rows: [Row!] @shareable total: Int }
            type Row @shareable { price: Int }
            """,
            """{ "data": { "rows": [{ "price": 3 }], "total": 4 } }""",
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices)],
            configureGatewayBuilder: builder => builder.AddDiagnosticEventListener(_ => order));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ rows { name price } total }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }
}

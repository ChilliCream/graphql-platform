using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData("ABC", ErrorHandlingMode.Propagate)]
    [InlineData("BCA", ErrorHandlingMode.Propagate)]
    [InlineData("CAB", ErrorHandlingMode.Propagate)]
    [InlineData("ABC", ErrorHandlingMode.Null)]
    [InlineData("BCA", ErrorHandlingMode.Null)]
    [InlineData("CAB", ErrorHandlingMode.Null)]
    public async Task Execute_Should_KeepSibling_When_SubgraphReturnsAnAlreadyNulledObject(
        string arrivalOrder,
        ErrorHandlingMode onError)
    {
        // arrange
        var order = new SourceResultOrder(arrivalOrder);
        using var names = CreateOrderedSubgraph(
            "A",
            """
            type Query { details: Details @shareable }
            type Details @shareable { name: String! }
            """,
            """
            {
              "data": { "details": null },
              "errors": [{ "message": "Name unavailable.", "path": ["details", "name"] }]
            }
            """,
            order);
        using var prices = CreateOrderedSubgraph(
            "B",
            """
            type Query { details: Details @shareable }
            type Details @shareable { price: Int }
            """,
            """{ "data": { "details": { "price": 3 } } }""",
            order);
        using var stock = CreateOrderedSubgraph(
            "C",
            """
            type Query { details: Details @shareable total: Int }
            type Details @shareable { available: Boolean }
            """,
            """{ "data": { "details": { "available": true }, "total": 4 } }""",
            order);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", names), ("B", prices), ("C", stock)],
            configureGatewayBuilder: builder => builder.AddDiagnosticEventListener(_ => order));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ details { name price available } total }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: arrivalOrder + "_OnError_" + onError);
    }
}

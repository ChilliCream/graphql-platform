using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportLookupError_When_LookupReturnsAnObject(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateProductCatalog();
        using var details = CreateProductDetailsSubgraph(
            """
            {
              "data": { "lookups": { "byId": { "label": "Mug" } } },
              "errors": [{ "message": "Lookup error.", "path": ["lookups", "byId"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              item: product { id label: description }
            }
            """,
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }
}

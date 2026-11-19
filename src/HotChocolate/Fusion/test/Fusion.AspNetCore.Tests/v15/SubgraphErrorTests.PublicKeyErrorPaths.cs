using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    public async Task Execute_Should_UsePublicPath_When_LookupKeyReportsAnError(
        ErrorHandlingMode onError,
        bool selectKey)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { product: Product }
            type Product @key(fields: "id") { id: Int! }
            """,
            """
            {
              "data": { "item": { "id": 1 } },
              "errors": [{ "message": "Key error.", "path": ["item", "id"] }]
            }
            """);
        using var details = CreateProductDetailsSubgraph(
            """{ "data": { "lookups": { "byId": { "label": "Mug" } } } }""");
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog), ("details", details)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            selectKey
                ? "{ item: product { id label: description } }"
                : "{ item: product { label: description } }",
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (selectKey ? "SelectedKey_OnError_" : "HiddenKey_OnError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }
}

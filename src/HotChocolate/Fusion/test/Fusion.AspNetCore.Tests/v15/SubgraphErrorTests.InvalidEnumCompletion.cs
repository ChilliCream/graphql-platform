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
    public async Task Execute_Should_MaskInvalidEnums_When_SiblingMayHaveAnError(
        ErrorHandlingMode onError,
        bool siblingError)
    {
        // arrange
        var responseBody = siblingError
            ? """
              {
                "data": { "color": 7, "colors": ["UNKNOWN"], "tail": null },
                "errors": [{ "message": "Tail unavailable.", "path": ["tail"] }]
              }
              """
            : """{ "data": { "color": 7, "colors": ["UNKNOWN"], "tail": null } }""";
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { color: Color colors: [Color!] tail: Int }
            enum Color { RED }
            """,
            responseBody);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ color colors tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (siblingError ? "SiblingError_OnError_" : "NoSiblingError_OnError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportFieldError_When_NestedRequiredEnumIsInvalid(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { product: Product productCount: Int! }
            type Product { availability: Availability! }
            enum Availability { IN_STOCK }
            """,
            """{ "data": { "product": { "status": "UNKNOWN" }, "productCount": 1 } }""");
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ product { status: availability } productCount }",
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

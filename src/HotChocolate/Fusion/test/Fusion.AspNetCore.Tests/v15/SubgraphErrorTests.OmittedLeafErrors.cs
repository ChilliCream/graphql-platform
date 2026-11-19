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
    public async Task Execute_Should_KeepSiblings_When_NestedRequiredFieldFails(
        ErrorHandlingMode onError,
        bool omitField)
    {
        // arrange
        var panel = omitField ? "{}" : """{ "n": null }""";
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { page: Page tail: String }
            type Page { panel: Panel keep: String }
            type Panel { name: String! }
            """,
            $$"""
            {
              "data": { "page": { "panel": {{panel}}, "keep": "K" }, "tail": "T" },
              "errors": [{ "message": "Name unavailable.", "path": ["page", "panel", "n"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            "{ page { panel { n: name } keep } tail }",
            onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (omitField ? "Omitted_OnError_" : "ExplicitNull_OnError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Execute_Should_ReportRootFieldError_When_SubgraphOmitsFailedEnum(ErrorHandlingMode onError)
    {
        // arrange
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { availability: Availability! tail: Int }
            enum Availability { IN_STOCK }
            """,
            """
            {
              "data": { "tail": 1 },
              "errors": [{ "message": "Availability unavailable.", "path": ["status"] }]
            }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ status: availability tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response, postFix: "OnError_" + onError);
    }
}

using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public partial class SubgraphErrorTests
{
    [Theory]
    [InlineData(ErrorHandlingMode.Propagate, false)]
    [InlineData(ErrorHandlingMode.Propagate, true)]
    [InlineData(ErrorHandlingMode.Null, false)]
    [InlineData(ErrorHandlingMode.Null, true)]
    public async Task Execute_Should_ReportItemIndex_When_SubgraphReturnsNullForRequiredObjectItem(
        ErrorHandlingMode onError,
        bool sourceReportsError)
    {
        // arrange
        var payload = sourceReportsError
            ? """
              {
                "data": { "r": [{ "value": 1 }, null], "tail": 4 },
                "errors": [{ "message": "Row unavailable.", "path": ["r", 1] }]
              }
              """
            : """{ "data": { "r": [{ "value": 1 }, null], "tail": 4 } }""";
        using var catalog = CreateSubgraph(
            "catalog",
            """
            type Query { rows: [Row!] tail: Int }
            type Row { value: Int }
            """,
            payload);
        using var gateway = await CreateCompositeSchemaAsync([("catalog", catalog)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ r: rows { value } tail }", onError: onError);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var postFix = (sourceReportsError ? "WithSourceError_" : "WithoutSourceError_") + onError;
        await MatchSnapshotAsync(gateway, request, response, postFix: postFix);
    }
}

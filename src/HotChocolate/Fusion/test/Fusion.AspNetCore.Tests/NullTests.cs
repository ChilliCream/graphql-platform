using System.Net;
using System.Text;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class NullTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ReportNonNullViolation_When_SourceReturnsNullForRequiredField()
    {
        // arrange
        using var source = CreateSourceSchema(
            "A",
            "type Query { nonNullString: String! }",
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """{"data":{"nonNullString":null}}""", Encoding.UTF8, "application/json")
                }));
        using var gateway = await CreateCompositeSchemaAsync([("A", source)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest("{ nonNullString }");

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }
}

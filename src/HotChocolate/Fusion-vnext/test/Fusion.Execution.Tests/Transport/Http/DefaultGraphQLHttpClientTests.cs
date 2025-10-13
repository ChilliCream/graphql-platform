namespace HotChocolate.Fusion.Transport.Http;

public class DefaultGraphQLHttpClientTests
{
    [Fact]
    public async Task Fetch_Large_Json()
    {
        // arrange
        var context = await GraphQLServerHelper.CreateTestServer();
        using var server = context.Item1;
        await using var app = context.Item2;
        using var client = new DefaultGraphQLHttpClient(server.CreateClient(), disposeInnerClient: true);

        var operationRequest = new HotChocolate.Transport.OperationRequest("{ items }");
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        using var document = await result.ReadAsResultAsync();

        // assert
        var itemCount = document.Root.GetProperty("data").GetProperty("items").GetArrayLength();
        Assert.Equal(500000, itemCount);

        await app.StopAsync();
    }
}

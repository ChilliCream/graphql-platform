using HotChocolate.Transport;

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

    [Fact]
    public async Task Read_Apollo_Request_Batching_Array_Response()
    {
        // arrange
        var handler = new MockHttpMessageHandler(
            """
            [
              {
                "data": {
                  "number": 0
                }
              },
              {
                "data": {
                  "number": 1
                }
              }
            ]
            """,
            "application/json");
        using var client = new DefaultGraphQLHttpClient(new HttpClient(handler));

        var operationRequest = new HotChocolate.Transport.OperationBatchRequest(
            [
                new OperationRequest("{ number }"),
                new OperationRequest("{ number }")
            ]);
        var request = new GraphQLHttpRequest(operationRequest, new Uri("http://localhost:5000/graphql"));

        // act
        using var result = await client.SendAsync(request);
        var stream = result.ReadAsResultStreamAsync();

        // assert
        var count = 0;

        await foreach (var document in stream)
        {
            var number = document.Root.GetProperty("data").GetProperty("number").GetInt32();

            Assert.Equal(count, number);

            document.Dispose();

            count++;
        }

        Assert.Equal(2, count);
    }

    private class MockHttpMessageHandler(string responseContent, string contentType) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, System.Text.Encoding.UTF8, contentType)
            };
            return Task.FromResult(response);
        }
    }
}

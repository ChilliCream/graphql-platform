using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion.Aspire.Nitro;

public sealed class NitroStageUpdateClientTests
{
    [Fact]
    public async Task SubscribeAndQuery_Should_PreserveEventPublishedDuringQueryRace()
    {
        // arrange
        var handler = new ResponseSequenceHandler(
            Sse(
                """
                event: next
                data: {"data":{"onStageChanged":{"__typename":"ClientVersionPublishedStageChangeEvent","kind":"CLIENT","clientVersion":{"id":"client-version-2","client":{"id":"client-1"}}}}}

                event: complete

                """),
            Json(
                """
                {
                  "data": {
                    "apiById": {
                      "stage": {
                        "publishedFusionConfiguration": {
                          "id": "configuration-1"
                        },
                        "publishedClients": [
                          {
                            "client": {
                              "id": "client-1"
                            },
                            "publishedVersions": [
                              {
                                "version": {
                                  "id": "client-version-2"
                                }
                              }
                            ]
                          }
                        ]
                      }
                    }
                  }
                }
                """));
        using var httpClient = new HttpClient(handler);
        var client = new NitroStageUpdateClient(
            GraphQLHttpClient.Create(httpClient, disposeHttpClient: false));
        var connection = new NitroConnection(
            new Uri("https://nitro.example.test"),
            new Uri("https://nitro.example.test/graphql"),
            NitroCredential.FromApiKey("secret"));

        // act
        await using var subscription = await client.SubscribeAsync(
            connection,
            "api-1",
            "production",
            TestContext.Current.CancellationToken);
        var snapshot = await client.GetLatestSnapshotAsync(
            connection,
            "api-1",
            "production",
            TestContext.Current.CancellationToken);
        var changes = new List<NitroStageChange>();
        await foreach (var change in subscription.ReadChangesAsync(
            TestContext.Current.CancellationToken))
        {
            changes.Add(change);
        }

        // assert
        var changedSnapshot = snapshot!.Apply(Assert.Single(changes));
        $"""
        Requests: {handler.Requests.Count}
        First operation: {ReadOperation(handler.Requests[0].Body)}
        Second operation: {ReadOperation(handler.Requests[1].Body)}
        API key sent: {handler.Requests.All(request => request.ApiKey == "secret")}
        Identity unchanged: {changedSnapshot.Identity == snapshot.Identity}
        """.MatchInlineSnapshot(
            """
            Requests: 2
            First operation: WatchNitroStage
            Second operation: GetNitroStageVersion
            API key sent: True
            Identity unchanged: True
            """);
    }

    private static string ReadOperation(string body)
    {
        using var document = System.Text.Json.JsonDocument.Parse(body);
        return document.RootElement.GetProperty("operationName").GetString()!;
    }

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage Sse(string body)
    {
        var content = new StringContent(body, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream")
        {
            CharSet = "utf-8"
        };

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private sealed class ResponseSequenceHandler(params HttpResponseMessage[] responses)
        : HttpMessageHandler
    {
        private readonly ConcurrentQueue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedGraphQLRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(
                new RecordedGraphQLRequest(
                    await request.Content!.ReadAsStringAsync(cancellationToken),
                    request.Headers.TryGetValues(NitroRequestHeaders.ApiKey, out var values)
                        ? values.Single()
                        : null));

            return _responses.TryDequeue(out var response)
                ? response
                : new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }

    private sealed record RecordedGraphQLRequest(string Body, string? ApiKey);
}

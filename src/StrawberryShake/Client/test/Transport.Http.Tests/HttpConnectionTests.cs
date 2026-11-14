using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore.Tests.Utilities;
using StrawberryShake.Json;

namespace StrawberryShake.Transport.Http;

public class HttpConnectionTests : ServerTestBase
{
    public HttpConnectionTests(TestServerFactory serverFactory)
        : base(serverFactory)
    {
    }

    [Fact]
    public async Task Simple_Request()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument("query Test { __typename }");
        var request = new OperationRequest("Test", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        Assert.Collection(
            results,
            t => t.RootElement.ToString().MatchSnapshot());
    }

    [Fact]
    public async Task ExecuteAsync_Should_RequestTheLegacyIncrementalFormat_When_Executing()
    {
        // arrange
        var handler = new StubHandler("application/graphql-response+json", """{"data":{"__typename":"Query"}}""");
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/graphql") };
        var request = new OperationRequest("Test", new MockDocument("query Test { __typename }"));
        var connection = new HttpConnection(() => client);

        // act
        await foreach (var _ in connection.ExecuteAsync(request))
        {
        }

        // assert
        handler.Accept.MatchInlineSnapshot(
            "application/graphql-response+json, application/json; q=0.9, "
            + "text/event-stream; incrementalSpec=v0.1, "
            + "application/graphql-response+jsonl; incrementalSpec=v0.1");
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkIncrementalPayloadsAsPatches_When_ResponseIsDeferred()
    {
        // arrange
        var handler = new StubHandler(
            "application/graphql-response+jsonl",
            """
            {"data":{"hero":{"name":"R2-D2"}},"hasNext":true}
            {"incremental":[{"data":{"id":"1"},"path":["hero"]}],"hasNext":false}

            """);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/graphql") };
        var request = new OperationRequest("Test", new MockDocument("query Test { hero { name } }"));
        var connection = new HttpConnection(() => client);

        // act
        var results = new List<string>();
        await foreach (var response in connection.ExecuteAsync(request))
        {
            results.Add($"{response.IsPatch} {response.HasNext} {response.Body!.RootElement}");
        }

        // assert
        results.MatchInlineSnapshots(
        [
            """
            False True {"data":{"hero":{"name":"R2-D2"}}}
            """,
            """
            True False {"incremental":[{"data":{"id":"1"},"path":["hero"]}]}
            """
        ]);
    }

    [Fact]
    public async Task ExecuteAsync_Should_MarkTrailingHasNextPayloadAsPatch_When_ResponseIsDeferred()
    {
        // arrange
        var handler = new StubHandler(
            "application/graphql-response+jsonl",
            """
            {"data":{"hero":{"name":"R2-D2"}},"hasNext":true}
            {"incremental":[{"data":{"id":"1"},"path":["hero"]}],"hasNext":true}
            {"hasNext":false}

            """);
        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5000/graphql") };
        var request = new OperationRequest("Test", new MockDocument("query Test { hero { name } }"));
        var connection = new HttpConnection(() => client);

        // act
        var results = new List<string>();
        await foreach (var response in connection.ExecuteAsync(request))
        {
            results.Add($"{response.IsPatch} {response.HasNext} {response.Body!.RootElement}");
        }

        // assert
        results.MatchInlineSnapshots(
        [
            """
            False True {"data":{"hero":{"name":"R2-D2"}}}
            """,
            """
            True True {"incremental":[{"data":{"id":"1"},"path":["hero"]}]}
            """,
            """
            True False {}
            """
        ]);
    }

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            """
            query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                        ... HeroAppearsIn2 @defer(label: "HeroAppearsIn2")
                    }
                }
            }

            fragment HeroAppearsIn2 on Character {
                appearsIn
            }
            """);
        var request = new OperationRequest("GetHero", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        var snapshot = Snapshot.Create();

        var i = 0;
        foreach (var result in results.OrderBy(
            r => r.RootElement.GetPropertyOrNull("path")?.ToString()))
        {
            // The order of the patches is not guaranteed, that is why we normalize the order and
            // normalize the hasNext... overall the guarantee of patchability lies with the server.
            snapshot.Add(
                result.RootElement
                    .ToString()
                    .Replace("\"hasNext\":false", "\"hasNext\":true"),
                $"Result {++i}");
        }

        await snapshot.MatchAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            """
            query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                    ... HeroAppearsIn @defer(label: "HeroAppearsIn")
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                    }
                }
            }

            fragment HeroAppearsIn on Character {
                appearsIn
            }
            """);
        var request = new OperationRequest("GetHero", document);

        // act
        var results = new List<JsonDocument>();
        var connection = new HttpConnection(() => client);
        await foreach (var response in connection.ExecuteAsync(request))
        {
            if (response.Body is not null)
            {
                results.Add(response.Body);
            }
        }

        // assert
        var snapshot = Snapshot.Create();

        var i = 0;
        foreach (var result in results)
        {
            snapshot.Add(result.RootElement.ToString(), $"Result {++i}");
        }

        await snapshot.MatchAsync(TestContext.Current.CancellationToken);
    }

    private sealed class StubHandler(string contentType, string body) : HttpMessageHandler
    {
        public string? Accept { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Accept = request.Headers.Accept.ToString();

            var content = new StringContent(body);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

            return Task.FromResult(new HttpResponseMessage { Content = content });
        }
    }

    private sealed class MockDocument : IDocument
    {
        private readonly byte[] _query;

        public MockDocument([StringSyntax("graphql")] string query)
        {
            _query = Encoding.UTF8.GetBytes(query);
        }

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => _query;

        public DocumentHash Hash { get; } = new("MD5", "ABC");
    }
}

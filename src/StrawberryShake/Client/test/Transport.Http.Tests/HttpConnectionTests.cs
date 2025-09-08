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

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            @"query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                }
            }

            fragment HeroName on Character {
                name
                friends {
                    nodes {
                        name
                        ... HeroAppearsIn2 @defer(label: ""HeroAppearsIn2"")
                    }
                }
            }

            fragment HeroAppearsIn2 on Character {
                appearsIn
            }");
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

        await snapshot.MatchAsync();
    }

    [Fact(Skip = "We are postponing defer integration until spec is more stable.")]
    public async Task MultiPart_Request_2()
    {
        // arrange
        var server = CreateStarWarsServer();
        var client = server.CreateClient();
        client.BaseAddress = new Uri("http://localhost:5000/graphql");

        var document = new MockDocument(
            @"query GetHero {
                hero(episode: NEW_HOPE) {
                    ... HeroName
                    ... HeroAppearsIn @defer(label: ""HeroAppearsIn"")
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
            }");
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

        await snapshot.MatchAsync();
    }

    private sealed class MockDocument : IDocument
    {
        private readonly byte[] _query;

        public MockDocument(string query)
        {
            _query = Encoding.UTF8.GetBytes(query);
        }

        public OperationKind Kind => OperationKind.Query;

        public ReadOnlySpan<byte> Body => _query;

        public DocumentHash Hash { get; } = new("MD5", "ABC");
    }
}

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.TestHost;
using Snapshooter.Xunit;
using Xunit;

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
                    ... HeroAppearsIn @defer(label: ""HeroAppearsIn"")
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

            fragment HeroAppearsIn on Character {
                appearsIn
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
        var i = 0;
        var data = new StringBuilder();

        foreach (var result in results)
        {
            data.Append("Result ").Append(++i).AppendLine(":");
            data.AppendLine(result.RootElement.ToString());
            data.AppendLine();
        }

        data.ToString().MatchSnapshot();
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

using System.Text;
using System.Text.Json;
using CookieCrumble;
using HotChocolate.AspNetCore.Tests.Utilities;
using static HotChocolate.AspNetCore.Tests.Utilities.TestServerExtensions;

namespace HotChocolate.Transport.Http;

public class VariableBatchRequestTestss(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Fact]
    public async Task Should_WriteNullValues()
    {
        // arrange
        var request = new VariableBatchRequest(
            null,
            "abc",
            "myOperation",
            variables:
            [
                new Dictionary<string, object?>
                {
                    ["abc"] = "def",
                    ["hij"] = null,
                },
                new Dictionary<string, object?>
                {
                    ["abc"] = "xyz",
                    ["hij"] = null,
                },
            ]);

        using var memory = new MemoryStream();
        await using var writer = new Utf8JsonWriter(memory);

        // act
        request.WriteTo(writer);
        await writer.FlushAsync();

        // assert
        var result = JsonDocument.Parse(Encoding.UTF8.GetString(memory.ToArray())).RootElement;
        result.MatchInlineSnapshot(
            """
            {
              "id": "abc",
              "operationName": "myOperation",
              "variables": [
                {
                  "abc": "def",
                  "hij": null
                },
                {
                  "abc": "xyz",
                  "hij": null
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Post_Variable_Batch()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables1 = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var variables2 = new Dictionary<string, object?>
        {
            ["episode"] = "EMPIRE",
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var request = new VariableBatchRequest(
            query,
            variables: new[] { variables1, variables2 });

        var response = await client.PostAsync(request, requestUri, cts.Token);

        // assert
        var snapshot = new Snapshot();

        await foreach(var result in response.ReadAsResultStreamAsync(cts.Token))
        {
            snapshot.Add(result);
        }

        snapshot.MatchInline(
            """
            ---------------
            VariableIndex: 0
            Data: {"hero":{"name":"R2-D2"}}
            ---------------

            ---------------
            VariableIndex: 1
            Data: {"hero":{"name":"Luke Skywalker"}}
            ---------------

            """);
    }

    [Fact]
    public async Task Post_Request_With_Nested_Variable_Batch()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables1 = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var variables2 = new Dictionary<string, object?>
        {
            ["episode"] = "EMPIRE",
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var nestedVariableBatchRequest = new VariableBatchRequest(
            query,
            variables: new[] { variables1, variables2 });

        var nestedSingleRequest = new OperationRequest(
            """
            {
                __typename
            }
            """);

        var batch = new OperationBatchRequest([nestedVariableBatchRequest, nestedSingleRequest]);

        var response = await client.PostAsync(batch, requestUri, cts.Token);

        // assert
        response.EnsureSuccessStatusCode();

        var sortedResults = new SortedList<(int?, int?), OperationResult>();

        await foreach(var result in response.ReadAsResultStreamAsync(cts.Token))
        {
            sortedResults.Add((result.RequestIndex, result.VariableIndex), result);
        }

        var snapshot = new Snapshot();

        foreach (var item in sortedResults.Values)
        {
            snapshot.Add(item);
        }

        snapshot.MatchInline(
            """
            ---------------
            RequestIndex: 0
            VariableIndex: 0
            Data: {"hero":{"name":"R2-D2"}}
            ---------------

            ---------------
            RequestIndex: 0
            VariableIndex: 1
            Data: {"hero":{"name":"Luke Skywalker"}}
            ---------------

            ---------------
            RequestIndex: 1
            Data: {"__typename":"Query"}
            ---------------

            """);
    }

    [Fact]
    public async Task Post_Request_Batch()
    {
        // arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1000));
        using var testServer = CreateStarWarsServer();
        var httpClient = testServer.CreateClient();
        var client = new DefaultGraphQLHttpClient(httpClient);

        var query =
            """
            query($episode: Episode!) {
              hero(episode: $episode) {
                name
              }
            }
            """;

        var variables1 = new Dictionary<string, object?>
        {
            ["episode"] = "JEDI",
        };

        var variables2 = new Dictionary<string, object?>
        {
            ["episode"] = "EMPIRE",
        };

        var requestUri = new Uri(CreateUrl("/graphql"));

        // act
        var request1 = new OperationRequest(
            query,
            variables: variables1);

        var request2 = new OperationRequest(
            query,
            variables: variables2);

        var request3 = new OperationRequest(
            """
            {
                __typename
            }
            """);

        var batch = new OperationBatchRequest([request1, request2, request3]);

        var response = await client.PostAsync(batch, requestUri, cts.Token);

        // assert
        response.EnsureSuccessStatusCode();

        var sortedResults = new SortedList<(int?, int?), OperationResult>();

        await foreach(var result in response.ReadAsResultStreamAsync(cts.Token))
        {
            sortedResults.Add((result.RequestIndex, result.VariableIndex), result);
        }

        var snapshot = new Snapshot();

        snapshot.Add(response.ContentHeaders.ContentType?.ToString(), "ContentType");

        foreach (var item in sortedResults.Values)
        {
            snapshot.Add(item);
        }

        snapshot.MatchInline(
            """
            ContentType
            ---------------
            text/event-stream; charset=utf-8
            ---------------

            ---------------
            RequestIndex: 0
            Data: {"hero":{"name":"R2-D2"}}
            ---------------

            ---------------
            RequestIndex: 1
            Data: {"hero":{"name":"Luke Skywalker"}}
            ---------------

            ---------------
            RequestIndex: 2
            Data: {"__typename":"Query"}
            ---------------

            """);
    }
}

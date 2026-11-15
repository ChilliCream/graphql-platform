using System.Text;
using System.Text.Json;
using HotChocolate.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequestBatchingTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_WriteRequestIndex_When_RequestBatchIsSent()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ],
            configureGatewayBuilder: b => b
                .ModifyServerOptions(o => o.Batching = AllowedBatching.All),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var request = CreateBatchRequest(
            """
            [
              { "query": "{ field(input: \"slow\") }" },
              { "query": "{ field(input: \"fast\") }" }
            ]
            """);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        ReadResultsByRequestIndex(body).MatchInlineSnapshots(
            [
                """
                {
                  "requestIndex": 0,
                  "data": {
                    "field": "slow"
                  }
                }
                """,
                """
                {
                  "requestIndex": 1,
                  "data": {
                    "field": "fast"
                  }
                }
                """
            ]);
    }

    [Fact]
    public async Task Execute_Should_WriteRequestIndex_When_ItemFailsValidation()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            r => r.AddQueryType<SourceSchema.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA)
            ],
            configureGatewayBuilder: b => b
                .ModifyServerOptions(o => o.Batching = AllowedBatching.All),
            includeOperationPlan: false);

        using var client = gateway.CreateClient();

        // act
        using var request = CreateBatchRequest(
            """
            [
              { "query": "{ field(input: \"fast\") }" },
              { "query": "{ unknownField }" }
            ]
            """);

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // assert
        ReadResultsByRequestIndex(body).MatchInlineSnapshots(
            [
                """
                {
                  "requestIndex": 0,
                  "data": {
                    "field": "fast"
                  }
                }
                """,
                """
                {
                  "requestIndex": 1,
                  "errors": [
                    {
                      "message": "The field `unknownField` does not exist on the type `Query`.",
                      "locations": [
                        {
                          "line": 1,
                          "column": 3
                        }
                      ],
                      "extensions": {
                        "type": "Query",
                        "field": "unknownField",
                        "responseName": "unknownField",
                        "specifiedBy": "https://spec.graphql.org/September2025/#sec-Field-Selections"
                      }
                    }
                  ]
                }
                """
            ]);
    }

    private static IEnumerable<object?> ReadResultsByRequestIndex(string body)
        => body
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<JsonElement>(line))
            .OrderBy(
                result => result.TryGetProperty("requestIndex", out var index)
                    ? index.GetInt32()
                    : -1)
            .Select(result => (object?)result);

    private static HttpRequestMessage CreateBatchRequest(string body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri("http://localhost:5000/graphql"))
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        request.Headers.Add("Accept", "application/jsonl");

        return request;
    }

    public static class SourceSchema
    {
        public class Query
        {
            // A slow result completes after a fast one, so results arrive out of request order.
            public async Task<string> GetField(string input)
            {
                if (input == "slow")
                {
                    await Task.Delay(250);
                }

                return input;
            }
        }
    }
}

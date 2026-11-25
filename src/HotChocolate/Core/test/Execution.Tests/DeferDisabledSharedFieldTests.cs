using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferDisabledSharedFieldTests
{
    private const string DisabledFirstQuery =
        """
        query ($d: Boolean!) {
            a {
                ... @defer(if: $d) { x }
                ... @defer { x y }
            }
        }
        """;

    private const string DisabledLastQuery =
        """
        query ($d: Boolean!) {
            a {
                ... @defer { x y }
                ... @defer(if: $d) { x }
            }
        }
        """;

    private const string PlainQuery =
        """
        {
            a {
                x
                y
            }
        }
        """;

    [Fact]
    public async Task Defer_Should_DeliverSharedFieldInInitialResponse_When_DisabledFragmentComesFirst()
    {
        // act
        var (payloads, merged, plain) = await ExecuteAsync(DisabledFirstQuery);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "a": {
                      "x": "x 1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "a"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                """
                {
                  "incremental": [
                    {
                      "id": "2",
                      "data": {
                        "y": "y 1"
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "2"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Equal(plain, merged);
    }

    [Fact]
    public async Task Defer_Should_DeliverSharedFieldInInitialResponse_When_DisabledFragmentComesLast()
    {
        // act
        var (payloads, merged, plain) = await ExecuteAsync(DisabledLastQuery);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "a": {
                      "x": "x 1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "a"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                """
                {
                  "incremental": [
                    {
                      "id": "2",
                      "data": {
                        "y": "y 1"
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "2"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Equal(plain, merged);
    }

    [Fact]
    public async Task Defer_Should_CompleteStream_When_OuterDeferIsDisabledAndInnerDeferIsNot()
    {
        // arrange
        var gates = new Gates();
        gates.X.SetResult();

        var executor = await new ServiceCollection()
            .AddSingleton(gates)
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var payloads = new List<string>();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($e: Boolean!) {
                        a {
                            ... @defer(if: $e) { ... @defer { y } x }
                            ... @defer { x }
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { ["e"] = false })
                .Build(),
            cts.Token);

        await using (var stream = Assert.IsType<ResponseStream>(result))
        {
            var read = Task.Run(
                async () =>
                {
                    await foreach (var payload in stream.ReadResultsAsync())
                    {
                        await using var cleanup = payload;
                        payloads.Add(payload.ToJson());
                        gates.Y.TrySetResult();
                    }
                },
                CancellationToken.None);

            await read.WaitAsync(cts.Token);
        }

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "a": {
                      "x": "x 1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "a"
                      ]
                    }
                  ],
                  "hasNext": true
                }
                """,
                """
                {
                  "incremental": [
                    {
                      "id": "2",
                      "data": {
                        "y": "y 1"
                      }
                    }
                  ],
                  "completed": [
                    {
                      "id": "2"
                    }
                  ],
                  "hasNext": false
                }
                """
            ]);
        Assert.Equal("""{"a":{"x":"x 1","y":"y 1"}}""", Merge(payloads).ToJsonString());
    }

    private static async Task<(List<string> Payloads, string Merged, string Plain)> ExecuteAsync(
        string query)
    {
        // arrange
        // y waits for the initial payload, so the order of the payloads is fixed.
        var gates = new Gates();
        gates.X.SetResult();

        var executor = await new ServiceCollection()
            .AddSingleton(gates)
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var payloads = new List<string>();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(query)
                .SetVariableValues(new Dictionary<string, object?> { ["d"] = false })
                .Build(),
            cts.Token);

        await using (var stream = Assert.IsType<ResponseStream>(result))
        {
            await foreach (var payload in stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                await using var cleanup = payload;
                payloads.Add(payload.ToJson());
                gates.Y.TrySetResult();
            }
        }

        gates.Y.TrySetResult();

        var plainResult = await executor.ExecuteAsync(PlainQuery, cts.Token);
        var plain = JsonNode.Parse(plainResult.ExpectOperationResult().ToJson())!["data"]!.ToJsonString();

        return (payloads, Merge(payloads).ToJsonString(), plain);
    }

    private static JsonNode Merge(List<string> payloads)
    {
        var paths = new Dictionary<string, JsonArray>();
        JsonNode? data = null;

        foreach (var payload in payloads)
        {
            var node = JsonNode.Parse(payload)!;

            if (node["data"] is { } initial)
            {
                data = initial.DeepClone();
            }

            if (node["pending"] is JsonArray pending)
            {
                foreach (var entry in pending)
                {
                    paths[entry!["id"]!.GetValue<string>()] = (JsonArray)entry["path"]!.DeepClone();
                }
            }

            if (node["incremental"] is JsonArray incremental)
            {
                foreach (var entry in incremental)
                {
                    var target = data!;

                    foreach (var segment in paths[entry!["id"]!.GetValue<string>()])
                    {
                        target = target[segment!.GetValue<string>()]!;
                    }

                    MergeObject((JsonObject)target, (JsonObject)entry["data"]!);
                }
            }
        }

        return data!;
    }

    private static void MergeObject(JsonObject target, JsonObject source)
    {
        foreach (var (name, value) in source)
        {
            if (value is JsonObject sourceObject && target[name] is JsonObject targetObject)
            {
                MergeObject(targetObject, sourceObject);
            }
            else
            {
                target[name] = value!.DeepClone();
            }
        }
    }

    public class Gates
    {
        public TaskCompletionSource X { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Y { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public class Query
    {
        public A A => new();
    }

    public class A
    {
        public async Task<string> GetXAsync([Service] Gates gates, CancellationToken cancellationToken)
        {
            await gates.X.Task.WaitAsync(cancellationToken);
            return "x 1";
        }

        public async Task<string> GetYAsync([Service] Gates gates, CancellationToken cancellationToken)
        {
            await gates.Y.Task.WaitAsync(cancellationToken);
            return "y 1";
        }
    }
}

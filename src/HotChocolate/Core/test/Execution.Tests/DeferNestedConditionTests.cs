using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferNestedConditionTests
{
    private const string Query =
        """
        query ($a: Boolean!, $b: Boolean!) {
            message {
                id
                ... @defer(if: $a) {
                    ... @defer(if: $b) {
                        body
                    }
                }
            }
        }
        """;

    [Fact]
    public async Task Defer_Should_DeliverFieldWithOuterFragment_When_OuterIsEnabledAndInnerIsDisabled()
    {
        // act
        var payloads = await ExecuteAsync(a: true, b: false);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "message": {
                      "id": "m1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "message"
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
                        "body": "Hello"
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
    }

    [Fact]
    public async Task Defer_Should_DeliverFieldWithInnerFragment_When_OuterIsDisabledAndInnerIsEnabled()
    {
        // act
        var payloads = await ExecuteAsync(a: false, b: true);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "message": {
                      "id": "m1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "message"
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
                        "body": "Hello"
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
    }

    [Fact]
    public async Task Defer_Should_DeliverFieldWithInnerFragment_When_OuterAndInnerAreEnabled()
    {
        // act
        var payloads = await ExecuteAsync(a: true, b: true);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "message": {
                      "id": "m1"
                    }
                  },
                  "pending": [
                    {
                      "id": "2",
                      "path": [
                        "message"
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
                        "body": "Hello"
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
    }

    [Fact]
    public async Task Defer_Should_NotDeferAnything_When_OuterAndInnerAreDisabled()
    {
        // act
        var payloads = await ExecuteAsync(a: false, b: false);

        // assert
        payloads.MatchInlineSnapshots(
            [
                """
                {
                  "data": {
                    "message": {
                      "id": "m1",
                      "body": "Hello"
                    }
                  }
                }
                """
            ]);
    }

    private static async Task<List<string>> ExecuteAsync(bool a, bool b)
    {
        var bodyGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!a && !b)
        {
            bodyGate.SetResult();
        }

        var executor = await new ServiceCollection()
            .AddSingleton(bodyGate)
            .AddGraphQL()
            .AddQueryType<MessageQuery>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(Query)
                .SetVariableValues(new Dictionary<string, object?> { ["a"] = a, ["b"] = b })
                .Build(),
            cts.Token);

        var payloads = new List<string>();

        if (result is not ResponseStream stream)
        {
            payloads.Add(result.ExpectOperationResult().ToJson());
            return payloads;
        }

        await using (stream)
        {
            await foreach (var payload in stream.ReadResultsAsync().WithCancellation(cts.Token))
            {
                await using var cleanup = payload;
                payloads.Add(payload.ToJson());
                bodyGate.TrySetResult();
            }
        }

        return payloads;
    }

    public class MessageQuery
    {
        public Message GetMessage() => new();
    }

    public class Message
    {
        public string Id => "m1";

        public async Task<string> GetBodyAsync(
            [Service] TaskCompletionSource bodyGate,
            CancellationToken cancellationToken)
        {
            await bodyGate.Task.WaitAsync(cancellationToken);
            return "Hello";
        }
    }
}

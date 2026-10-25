using System.Text.Json;
using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferTests
{
    [Fact]
    public async Task VariableBatch_Defer_Should_Deliver_Payloads_Per_Item_When_Executed()
    {
        // arrange
        // a per-item gate makes the deferred split deterministic instead of racing wall-clock delays
        var nameGates = new Dictionary<string, TaskCompletionSource>
        {
            ["1"] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously),
            ["2"] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var executor = await CreateGatedDeferExecutorAsync(nameGates);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($id: Int!) {
                    person(id: $id) {
                        id
                        ... @defer {
                            name
                        }
                    }
                }
                """)
            .SetVariableValues(
                new List<IReadOnlyDictionary<string, object?>>
                {
                    new Dictionary<string, object?> { ["id"] = 1 },
                    new Dictionary<string, object?> { ["id"] = 2 }
                })
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cts.Token);
        var summaries = await SummarizeAllItemsConcurrentlyAsync(result, nameGates, cts.Token);

        // assert
        summaries.MatchInlineSnapshots(
        [
            "person.id=1; name=Pascal; hasNext=True,False",
            "person.id=2; name=Rafi; hasNext=True,False"
        ]);
    }

    private static ValueTask<IRequestExecutor> CreateGatedDeferExecutorAsync(
        IReadOnlyDictionary<string, TaskCompletionSource> nameGates)
        => new ServiceCollection()
            .AddSingleton(nameGates)
            .AddGraphQL()
            .AddQueryType<GatedDeferTestSchema.Query>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .BuildRequestExecutorAsync();

    // Item 0's stream only settles once every item's deferred branch has completed, so the items
    // are summarized concurrently: summarizing one to completion before starting the next would
    // leave its gate unset and deadlock the batch.
    private static Task<string[]> SummarizeAllItemsConcurrentlyAsync(
        IExecutionResult result,
        IReadOnlyDictionary<string, TaskCompletionSource> nameGates,
        CancellationToken cancellationToken)
        => Task.WhenAll(
            result.ExpectOperationResultBatch().Results
                .Select(item => SummarizeGatedStreamAsync(item.ExpectResponseStream(), nameGates, cancellationToken)));

    // Reads a single item's response stream to completion and projects it to a delivery-shape
    // summary that pins the initial data, the deferred data, and the per-payload hasNext sequence.
    // The corresponding name gate is released only once the initial payload has been observed, so
    // the deferred resolver cannot complete before the split it is meant to produce.
    private static async Task<string> SummarizeGatedStreamAsync(
        ResponseStream stream,
        IReadOnlyDictionary<string, TaskCompletionSource> nameGates,
        CancellationToken cancellationToken)
    {
        string? personId = null;
        string? name = null;
        var hasNext = new List<bool>();
        var initialPayloadObserved = false;

        await foreach (var payload in stream.ReadResultsAsync().WithCancellation(cancellationToken))
        {
            await using var payloadCleanup = payload;
            using var document = JsonDocument.Parse(payload.ToJson());
            var root = document.RootElement;

            hasNext.Add(root.TryGetProperty("hasNext", out var hasNextValue) && hasNextValue.GetBoolean());

            if (!initialPayloadObserved
                && root.TryGetProperty("data", out var data)
                && data.ValueKind is JsonValueKind.Object
                && data.TryGetProperty("person", out var person)
                && person.TryGetProperty("id", out var id))
            {
                personId = id.GetRawText();
                initialPayloadObserved = true;

                if (nameGates.TryGetValue(personId, out var gate))
                {
                    gate.TrySetResult();
                }
            }

            // The deferred name must arrive inside an incremental entry's data (the deferred
            // payload), never in the initial data. Reading only incremental[].data keeps the
            // non-deterministic branch identifiers carried elsewhere in the envelope out of
            // the summary.
            if (root.TryGetProperty("incremental", out var incremental)
                && incremental.ValueKind is JsonValueKind.Array)
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    if (entry.TryGetProperty("data", out var incrementalData)
                        && TryFindName(incrementalData, out var deferredName))
                    {
                        name = deferredName;
                    }
                }
            }
        }

        return $"person.id={personId}; name={name}; hasNext={string.Join(",", hasNext)}";
    }

    private static bool TryFindName(JsonElement element, out string? name)
    {
        if (element.ValueKind is JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name is "name" && property.Value.ValueKind is JsonValueKind.String)
                {
                    name = property.Value.GetString();
                    return true;
                }

                if (TryFindName(property.Value, out name))
                {
                    return true;
                }
            }
        }

        name = null;
        return false;
    }

    [Fact]
    public async Task InlineFragment_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    person(id: "UGVyc29uOjE=") {
                        id
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_Nested()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    person(id: "UGVyc29uOjE=") {
                        id
                        ... @defer {
                            name
                        }
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer(label: "abc") {
                    person(id: "UGVyc29uOjE=") {
                        id
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer(if: false) {
                    person(id: "UGVyc29uOjE=") {
                        id
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task InlineFragment_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($defer: Boolean!) {
                        ... @defer(if: $defer) {
                            person(id: "UGVyc29uOjE=") {
                                id
                            }
                        }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "defer", false }
                    })
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_Nested()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                    ... @defer {
                        name
                    }
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer(label: "abc")
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... Foo @defer(if: false)
            }

            fragment Foo on Query {
                person(id: "UGVyc29uOjE=") {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task FragmentSpread_Defer_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($defer: Boolean!) {
                        ... Foo @defer(if: $defer)
                    }

                    fragment Foo on Query {
                        person(id: "UGVyc29uOjE=") {
                            id
                        }
                    }
                    """)
                .SetVariableValues(
                    new Dictionary<string, object?>
                    {
                        { "defer", false }
                    })
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Stacked_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ... @defer {
                            ensureState {
                                ... @defer {
                                    state
                                }
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Stacked_Defer_2()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        await using var response = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ... @defer {
                            e: ensureState {
                                ... @defer {
                                    more {
                                        ... @defer {
                                            stuff
                                        }
                                    }
                                }
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(response).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Single_Defer()
    {
        // this test ensures that the request context is not recycled until the
        // a stream is fully processed when no outer DI scope exists.

        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ensureState {
                            ... @defer {
                                state
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Ensure_GlobalState_Is_Passed_To_DeferContext_Single_Defer_2()
    {
        // this test ensures that the request context is not recycled until the
        // a stream is fully processed when an outer DI scope exists.

        // arrange
        var services = DeferAndStreamTestSchema.CreateServiceProvider();
        var executor = await services.GetRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        await using var scope = services.CreateAsyncScope();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    {
                        ... @defer {
                            ensureState {
                                state
                            }
                        }
                    }
                    """)
                .SetGlobalState("requestState", "state 123")
                .SetServices(scope.ServiceProvider)
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    private class StateRequestInterceptor : DefaultHttpRequestInterceptor
    {
        public override ValueTask OnCreateAsync(
            HttpContext context,
            IRequestExecutor requestExecutor,
            OperationRequestBuilder requestBuilder,
            CancellationToken cancellationToken)
        {
            requestBuilder.AddGlobalState("requestState", "bar");
            return base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
        }
    }
}

// A defer schema whose deferred field blocks on a per-item gate instead of a wall-clock delay, so
// tests can pin exactly when a deferred branch is allowed to complete.
internal static class GatedDeferTestSchema
{
    public class Query
    {
        private static readonly Dictionary<int, string> s_names = new() { [1] = "Pascal", [2] = "Rafi" };

        public Person GetPerson(int id) => new(id, s_names[id]);
    }

    public class Person(int id, string name)
    {
        public int Id { get; } = id;

        public async Task<string> GetNameAsync(
            [Service] IReadOnlyDictionary<string, TaskCompletionSource> nameGates,
            CancellationToken cancellationToken)
        {
            await nameGates[Id.ToString()].Task.WaitAsync(cancellationToken);
            return name;
        }
    }
}

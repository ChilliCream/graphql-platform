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
        // the timeout turns a lost-coordinator hang into a test failure instead of a block.
        var executor = await DeferAndStreamTestSchema.CreateAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        var request = OperationRequestBuilder
            .New()
            .SetDocument(
                """
                query ($id: ID!) {
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
                    new Dictionary<string, object?> { ["id"] = "UGVyc29uOjE=" },
                    new Dictionary<string, object?> { ["id"] = "UGVyc29uOjI=" }
                })
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, cts.Token);

        var summaries = new List<string>();
        foreach (var item in result.ExpectOperationResultBatch().Results)
        {
            summaries.Add(await SummarizeStreamAsync(item.ExpectResponseStream(), cts.Token));
        }

        // assert
        summaries.MatchInlineSnapshots(
        [
            "person.id=UGVyc29uOjE=; name=Pascal; hasNext=True,False",
            "person.id=UGVyc29uOjI=; name=Rafi; hasNext=True,False"
        ]);
    }

    // Reads a single item's response stream to completion and projects it to a delivery-shape
    // summary that pins the initial data, the deferred data, and the per-payload hasNext sequence
    // without depending on the non-deterministic branch identifiers in the incremental envelope.
    private static async Task<string> SummarizeStreamAsync(
        ResponseStream stream,
        CancellationToken cancellationToken)
    {
        string? personId = null;
        string? name = null;
        var hasNext = new List<bool>();

        await foreach (var payload in stream.ReadResultsAsync().WithCancellation(cancellationToken))
        {
            await using var payloadCleanup = payload;
            using var document = JsonDocument.Parse(payload.ToJson());
            var root = document.RootElement;

            hasNext.Add(root.TryGetProperty("hasNext", out var hasNextValue) && hasNextValue.GetBoolean());

            if (root.TryGetProperty("data", out var data)
                && data.ValueKind is JsonValueKind.Object
                && data.TryGetProperty("person", out var person)
                && person.TryGetProperty("id", out var id))
            {
                personId = id.GetString();
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

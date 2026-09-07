using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Covers the incremental delivery of streamed lists: the initial slice, the per-item entries,
/// the arguments of the stream directive and the fields that ignore the directive.
/// </summary>
public class StreamTests
{
    /// <summary>
    /// This test shows how IAsyncEnumerable is translated to SDL
    /// </summary>
    [Fact]
    public async Task Schema()
    {
        var executor = await DeferAndStreamTestSchema.CreateAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Should_DeliverEachItemInItsOwnPayload_When_SourceExceedsInitialCount()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // the initial slice and the look-ahead item must be available before execution starts.
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { name } }",
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        // every item completes only after the previous payload was read, so the payload
        // boundaries are the delivery boundaries and not an artifact of coalescing.
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Write("c");
        source.Release("c");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"c"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverAnEmptyListAndEveryItemAsEntry_When_InitialCountIsZero()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream { name } }",
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("a");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"a"}]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_CompleteInline_When_SourceIsExhaustedAtInitialCount()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(initialCount: 4) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        // the look-ahead found no further item, so the whole list is completed inline
        // and no stream branch is announced.
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"persons":[{"id":"UGVyc29uOjE="},{"id":"UGVyc29uOjI="},{"id":"UGVyc29uOjM="},{"id":"UGVyc29uOjQ="}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_CompleteInline_When_InitialCountExceedsItemCount()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(initialCount: 7) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"persons":[{"id":"UGVyc29uOjE="},{"id":"UGVyc29uOjI="},{"id":"UGVyc29uOjM="},{"id":"UGVyc29uOjQ="}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_AnnounceTheLabel_When_LabelIsSet()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        var result = await executor.ExecuteAsync(
            """{ items @stream(initialCount: 1, label: "abc") { name } }""",
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"],"label":"abc"}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_SliceTheList_When_InitialCountIsAVariable()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query($initialCount: Int!) {
                        items @stream(initialCount: $initialCount) {
                            name
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "initialCount", 1 } })
                .Build(),
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverInline_When_IfIsFalse()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            "{ persons @stream(if: false) { id } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"persons":[{"id":"UGVyc29uOjE="},{"id":"UGVyc29uOjI="},{"id":"UGVyc29uOjM="},{"id":"UGVyc29uOjQ="}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_DeliverInline_When_IfVariableIsFalse()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("query($stream: Boolean!) { persons @stream(if: $stream) { id } }")
                .SetVariableValues(new Dictionary<string, object?> { { "stream", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"persons":[{"id":"UGVyc29uOjE="},{"id":"UGVyc29uOjI="},{"id":"UGVyc29uOjM="},{"id":"UGVyc29uOjQ="}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_DeliverItemsAsEntries_When_IfVariableIsTrue()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    "query($stream: Boolean!) { items @stream(initialCount: 1, if: $stream) { name } }")
                .SetVariableValues(new Dictionary<string, object?> { { "stream", true } })
                .Build(),
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverInline_When_FieldIsAPlainList()
    {
        // arrange
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);

        // act
        // a plain list is not a stream source, so the directive is ignored.
        var result = await executor.ExecuteAsync(
            "{ plainItems @stream(initialCount: 1) { name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"plainItems":[{"name":"a"},{"name":"b"}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_DeliverInline_When_DirectiveIsNotUsed()
    {
        // arrange
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);
        source.Write("a");
        source.Write("b");
        source.Complete();

        // act
        var result = await executor.ExecuteAsync(
            "{ items { name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"data":{"items":[{"name":"a"},{"name":"b"}]}}
            """);
    }

    [Fact]
    public async Task Stream_Should_AnnouncePendingDefer_When_StreamedItemContainsDefer()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Release("a");

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { name ... @defer { upperName } } }",
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("a:upper");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b:upper");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}]},"pending":[{"id":"2","path":["items",0]},{"id":"3","path":["items"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","data":{"upperName":"A"}}],"completed":[{"id":"2"}],"hasNext":true}""",
            """{"pending":[{"id":"4","path":["items",1]}],"incremental":[{"id":"3","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"incremental":[{"id":"4","data":{"upperName":"B"}}],"completed":[{"id":"4"}],"hasNext":true}""",
            """{"completed":[{"id":"3"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_DeliverBothBranches_When_TwoFieldsAreStreamed()
    {
        // arrange
        var source = new StreamSource(gated: true);
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.WriteMore("x");
        source.WriteMore("y");
        source.Release("a");
        source.Release("x");

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { name } moreItems @stream(initialCount: 1) { name } }",
            cts.Token);

        var payloads = new List<string>();

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);

        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("b");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Release("y");
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.Complete();
        payloads.Add(await ReadPayloadAsync(enumerator));
        source.CompleteMore();
        payloads.Add(await ReadPayloadAsync(enumerator));

        // assert
        payloads.MatchInlineSnapshots(
        [
            """{"data":{"items":[{"name":"a"}],"moreItems":[{"name":"x"}]},"pending":[{"id":"2","path":["items"]},{"id":"3","path":["moreItems"]}],"hasNext":true}""",
            """{"incremental":[{"id":"2","items":[{"name":"b"}]}],"hasNext":true}""",
            """{"incremental":[{"id":"3","items":[{"name":"y"}]}],"hasNext":true}""",
            """{"completed":[{"id":"2"}],"hasNext":true}""",
            """{"completed":[{"id":"3"}],"hasNext":false}"""
        ]);
    }

    [Fact]
    public async Task Stream_Should_StreamOnlyTheOuterList_When_ItemsAreLists()
    {
        // arrange
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var result = await executor.ExecuteAsync(
            "{ matrix @stream(initialCount: 1) }",
            cts.Token);

        // assert
        // each entry carries a complete inner list, only the outer list is streamed.
        var delivery = await IncrementalDeliveryReader.ReadDeliveryAsync(result, cts.Token);
        delivery.MatchInlineSnapshots(
        [
            """initial:{"matrix":[[1,2]]}""",
            """items:[[3,4]]""",
            """items:[[5,6]]""",
            "end"
        ]);
    }

    [Fact]
    public async Task Stream_Should_ReportTheSameCost_When_DeliveryDirectivesAreUsed()
    {
        // arrange
        const string inlineDocument = "{ items { name } }";
        const string streamDocument = "{ items @stream(initialCount: 1) { name } }";
        const string streamAndDeferDocument =
            "{ items @stream(initialCount: 1) { ... @defer { name } } }";

        // act
        var inlineCost = await ReadOperationCostAsync(inlineDocument);
        var streamCost = await ReadOperationCostAsync(streamDocument);
        var streamAndDeferCost = await ReadOperationCostAsync(streamAndDeferDocument);

        // assert
        // cost analysis ignores the delivery directives, so the score is identical.
        Assert.Equal(inlineCost, streamCost);
        Assert.Equal(inlineCost, streamAndDeferCost);
        inlineCost.MatchInlineSnapshot("""{"fieldCost":20,"typeCost":2}""");
    }

    [Fact]
    public async Task Stream_Should_RejectTheRequest_When_StreamsAreNotAllowedAndStreamIsDisabledByVariable()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        // a request without stream support, as a transport that only accepts a single result sends it.
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument("query($stream: Boolean!) { persons @stream(if: $stream) { id } }")
                .SetVariableValues(new Dictionary<string, object?> { { "stream", false } })
                .SetFlags(RequestFlags.AllowQuery)
                .Build(),
            TestContext.Current.CancellationToken);

        // assert
        // the syntax scan marks the operation incremental because it cannot resolve the
        // variable, so the request is rejected although the list is delivered inline.
        Assert.IsType<OperationResult>(result).ToJson(withIndentations: false).MatchInlineSnapshot(
            """
            {"errors":[{"message":"The specified operation kind is not allowed."}]}
            """);
    }

    [Fact]
    public async Task Stream_Should_DisposeEnumeratorBeforeResolverScope_When_StreamCompletes()
    {
        // arrange
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        source.Write("a");
        source.Write("b");
        source.Complete();

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { name } }",
            cts.Token);

        await foreach (var payload in
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().WithCancellation(cts.Token))
        {
            await payload.DisposeAsync();
        }

        await source.CleanupCompleted.WaitAsync(cts.Token);

        // assert
        Assert.Equal(["enumerator", "scope"], source.Cleanup);
    }

    [Fact]
    public async Task Stream_Should_DisposeEnumeratorBeforeResolverScope_When_RequestIsCanceled()
    {
        // arrange
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var requestAborted = new CancellationTokenSource();
        source.Write("a");
        source.Write("b");

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { name } }",
            requestAborted.Token);

        await using var enumerator =
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().GetAsyncEnumerator(cts.Token);
        await ReadPayloadAsync(enumerator);

        // the source has no further item, so the stream task is parked in the enumeration
        // when the request is aborted.
        await requestAborted.CancelAsync();
        await source.CleanupCompleted.WaitAsync(cts.Token);

        // assert
        Assert.Equal(["enumerator", "scope"], source.Cleanup);
    }

    private static async Task<string> ReadPayloadAsync(IAsyncEnumerator<OperationResult> enumerator)
    {
        Assert.True(await enumerator.MoveNextAsync());

        await using var payload = enumerator.Current;
        return payload.ToJson(withIndentations: false);
    }

    private static async Task<string> ReadOperationCostAsync(string document)
    {
        var source = new StreamSource();
        var executor = await CreateStreamSourceExecutorAsync(source);
        source.Write("a");
        source.Write("b");
        source.Complete();

        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument(document).ReportCost().Build(),
            TestContext.Current.CancellationToken);

        if (result is ResponseStream responseStream)
        {
            await using var enumerator =
                responseStream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
            var payload = await ReadPayloadAsync(enumerator);
            return ReadOperationCost(payload);
        }

        return ReadOperationCost(Assert.IsType<OperationResult>(result).ToJson(withIndentations: false));
    }

    private static string ReadOperationCost(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.GetProperty("extensions").GetProperty("operationCost").GetRawText();
    }

    private static async Task<IRequestExecutor> CreateStreamSourceExecutorAsync(StreamSource source)
        => await new ServiceCollection()
            .AddSingleton(source)
            .AddScoped<ScopedProbe>()
            .AddGraphQL()
            .AddCostAnalyzer()
            .AddQueryType<StreamSourceQuery>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .BuildRequestExecutorAsync();

    public class StreamSourceQuery
    {
        public IAsyncEnumerable<StreamItem> GetItems(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();

            // the probe lives in the resolver scope, so its disposal marks the scope disposal.
            context.Services.GetRequiredService<ScopedProbe>();

            return ReadItemsAsync(source);
        }

        public IAsyncEnumerable<StreamItem> GetMoreItems(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();

            return ReadMoreItemsAsync(source);
        }

        public List<StreamItem> GetPlainItems() => [new StreamItem("a"), new StreamItem("b")];

        public async IAsyncEnumerable<List<int>> GetMatrix()
        {
            await Task.Yield();
            yield return [1, 2];
            yield return [3, 4];
            yield return [5, 6];
        }

        private static async IAsyncEnumerable<StreamItem> ReadItemsAsync(
            StreamSource source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var item in source.ReadAllAsync(cancellationToken))
                {
                    yield return new StreamItem(item);
                }
            }
            finally
            {
                source.Track("enumerator");
            }
        }

        private static async IAsyncEnumerable<StreamItem> ReadMoreItemsAsync(
            StreamSource source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in source.ReadAllMoreAsync(cancellationToken))
            {
                yield return new StreamItem(item);
            }
        }
    }

    public sealed class StreamItem(string name)
    {
        public async Task<string> GetNameAsync(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();
            await source.WaitAsync(name).WaitAsync(context.RequestAborted);
            return name;
        }

        public async Task<string> GetUpperNameAsync(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();
            await source.WaitAsync($"{name}:upper").WaitAsync(context.RequestAborted);
            return name.ToUpperInvariant();
        }
    }

    public sealed class ScopedProbe(StreamSource source) : IDisposable
    {
        public void Dispose() => source.Track("scope");
    }

    public sealed class StreamSource(bool gated = false)
    {
        private readonly Channel<string> _items = Channel.CreateUnbounded<string>();
        private readonly Channel<string> _moreItems = Channel.CreateUnbounded<string>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
        private readonly List<string> _cleanup = [];
        private readonly TaskCompletionSource _cleanupCompleted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CleanupCompleted => _cleanupCompleted.Task;

        public string[] Cleanup
        {
            get
            {
                lock (_cleanup)
                {
                    return [.. _cleanup];
                }
            }
        }

        public void Write(string name) => _items.Writer.TryWrite(name);

        public void WriteMore(string name) => _moreItems.Writer.TryWrite(name);

        public void Complete() => _items.Writer.TryComplete();

        public void CompleteMore() => _moreItems.Writer.TryComplete();

        public void Release(string name) => Gate(name).TrySetResult();

        public Task WaitAsync(string name) => gated ? Gate(name).Task : Task.CompletedTask;

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
            => _items.Reader.ReadAllAsync(cancellationToken);

        public IAsyncEnumerable<string> ReadAllMoreAsync(CancellationToken cancellationToken)
            => _moreItems.Reader.ReadAllAsync(cancellationToken);

        public void Track(string entry)
        {
            lock (_cleanup)
            {
                _cleanup.Add(entry);

                if (_cleanup.Count == 2)
                {
                    _cleanupCompleted.TrySetResult();
                }
            }
        }

        private TaskCompletionSource Gate(string name)
            => _gates.GetOrAdd(name, _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}

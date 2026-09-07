using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

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
    public async Task Stream()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Nested_Defer()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 800)
                }
                personNodes(first: 1) {
                    nodes @stream {
                        ... @defer {
                            name
                        }
                    }
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_InitialCount_Set_To_1()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 1) {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_InitialCount_Exceeds_Total_Count()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            @"{
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(initialCount: 7) {
                    id
                }
            }",
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_Label_Set_To_abc()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                ... @defer {
                    wait(m: 300)
                }
                persons @stream(label: "abc") {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<ResponseStream>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_If_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                persons @stream(if: false) {
                    id
                }
            }
            """,
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }

    [Fact]
    public async Task Stream_If_Variable_Set_To_false()
    {
        // arrange
        var executor = await DeferAndStreamTestSchema.CreateAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder
                .New()
                .SetDocument(
                    """
                    query ($stream: Boolean!) {
                        persons @stream(if: $stream) {
                            id
                        }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?> { { "stream", false } })
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
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

    private static async Task<IRequestExecutor> CreateStreamSourceExecutorAsync(StreamSource source)
        => await new ServiceCollection()
            .AddSingleton(source)
            .AddScoped<ScopedProbe>()
            .AddGraphQL()
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
    }

    public sealed class StreamItem(string name)
    {
        public async Task<string> GetNameAsync(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();
            await source.WaitAsync(name).WaitAsync(context.RequestAborted);
            return name;
        }
    }

    public sealed class ScopedProbe(StreamSource source) : IDisposable
    {
        public void Dispose() => source.Track("scope");
    }

    public sealed class StreamSource(bool gated = false)
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
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

        public void Write(string name) => _channel.Writer.TryWrite(name);

        public void Complete() => _channel.Writer.TryComplete();

        public void Release(string name) => Gate(name).TrySetResult();

        public Task WaitAsync(string name) => gated ? Gate(name).Task : Task.CompletedTask;

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);

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

    [Fact]
    public async Task AsyncEnumerable_Result()
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
                        persons {
                            id
                        }
                    }
                    """)
                .Build(),
            TestContext.Current.CancellationToken);

        Assert.IsType<OperationResult>(result).MatchSnapshot();
    }
}

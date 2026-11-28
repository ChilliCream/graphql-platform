using HotChocolate.Execution.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

public class VariableBatchContextPoolingTests
{
    [Fact]
    public async Task Execute_Should_ReturnAllContextsToPool_When_VariableBatchCompletes()
    {
        // arrange
        var pools = new RecordingPoolProvider();
        var executor = await CreateExecutorAsync(pools, new BatchGates());

        using var request = OperationRequestBuilder.New()
            .SetDocument("query($value: String!) { echo(value: $value) }")
            .SetVariableValues("""[{ "value": "a" }, { "value": "b" }, { "value": "c" }]""")
            .Build();

        // act
        await using var result = await executor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(3, pools.KeptContexts);
        Assert.Equal(0, pools.DiscardedContexts);
    }

    [Fact]
    public async Task Execute_Should_ReturnResultPerVariableSet_When_ContextsComeFromPool()
    {
        // arrange
        var pools = new RecordingPoolProvider();
        var executor = await CreateExecutorAsync(pools, new BatchGates());

        using var first = OperationRequestBuilder.New()
            .SetDocument("query($value: String!) { echo(value: $value) }")
            .SetVariableValues("""[{ "value": "a" }, { "value": "b" }]""")
            .Build();
        using var second = OperationRequestBuilder.New()
            .SetDocument("query($value: String!) { echo(value: $value) }")
            .SetVariableValues("""[{ "value": "c" }, { "value": "d" }]""")
            .Build();

        await (await executor.ExecuteAsync(first, TestContext.Current.CancellationToken)).DisposeAsync();

        // act
        await using var result = await executor.ExecuteAsync(second, TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, pools.CreatedContexts);
        result.ExpectOperationResultBatch().Results.MatchInlineSnapshots(
        [
            """
            {
              "variableIndex": 0,
              "data": {
                "echo": "c"
              }
            }
            """,
            """
            {
              "variableIndex": 1,
              "data": {
                "echo": "d"
              }
            }
            """
        ]);
    }

    [Fact]
    public async Task Execute_Should_KeepContextsOutOfPool_When_VariableBatchIsCanceled()
    {
        // arrange
        var pools = new RecordingPoolProvider();
        var gates = new BatchGates();
        var executor = await CreateExecutorAsync(pools, gates);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abort = new CancellationTokenSource();

        using var request = OperationRequestBuilder.New()
            .SetDocument("query($value: String!) { wait(value: $value) }")
            .SetVariableValues("""[{ "value": "a" }, { "value": "b" }]""")
            .Build();

        var execution = executor.ExecuteAsync(request, abort.Token);
        await gates.Entered("a").Task.WaitAsync(timeout.Token);
        await gates.Entered("b").Task.WaitAsync(timeout.Token);

        // act
        await abort.CancelAsync();
        await using var result = await execution.WaitAsync(timeout.Token);

        // assert
        Assert.Equal("HC0049", Assert.Single(result.ExpectOperationResult().Errors).Code);
        Assert.Equal(0, pools.KeptContexts);
        Assert.Equal(0, pools.DiscardedContexts);
    }

    [Fact]
    public async Task Execute_Should_DiscardContexts_When_DeferredVariableBatchIsAbortedWhileResolversRun()
    {
        // arrange
        var pools = new RecordingPoolProvider();
        var gates = new BatchGates();
        var executor = await CreateExecutorAsync(pools, gates);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abort = new CancellationTokenSource();

        using var request = OperationRequestBuilder.New()
            .SetDocument("query($value: String!) { echo(value: $value) ... @defer { slow(value: $value) } }")
            .SetVariableValues("""[{ "value": "a" }, { "value": "b" }]""")
            .Build();

        var result = await executor.ExecuteAsync(request, abort.Token);
        await gates.Entered("a").Task.WaitAsync(timeout.Token);
        await gates.Entered("b").Task.WaitAsync(timeout.Token);

        // act
        await abort.CancelAsync();
        await result.DisposeAsync();
        var kept = pools.KeptContexts;
        var discarded = pools.DiscardedContexts;
        gates.Release.SetResult();

        // assert
        Assert.Equal(0, kept);
        Assert.Equal(2, discarded);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(RecordingPoolProvider pools, BatchGates gates)
        => new ServiceCollection()
            .AddSingleton<ObjectPoolProvider>(pools)
            .AddSingleton(gates)
            .AddGraphQL()
            .AddQueryType<BatchQuery>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();

    /// <summary>
    /// Creates the default pools and counts what the pool of operation contexts creates, keeps and discards.
    /// </summary>
    private sealed class RecordingPoolProvider : ObjectPoolProvider
    {
        private readonly DefaultObjectPoolProvider _provider = new();
        private int _created;
        private int _kept;
        private int _discarded;

        public int CreatedContexts => Volatile.Read(ref _created);

        public int KeptContexts => Volatile.Read(ref _kept);

        public int DiscardedContexts => Volatile.Read(ref _discarded);

        public override ObjectPool<T> Create<T>(IPooledObjectPolicy<T> policy)
            => _provider.Create(
                typeof(T) == typeof(OperationContext)
                    ? new RecordingPolicy<T>(policy, this)
                    : policy);

        private sealed class RecordingPolicy<T>(IPooledObjectPolicy<T> policy, RecordingPoolProvider recorder)
            : IPooledObjectPolicy<T> where T : notnull
        {
            public T Create()
            {
                Interlocked.Increment(ref recorder._created);
                return policy.Create();
            }

            public bool Return(T obj)
            {
                var kept = policy.Return(obj);
                Interlocked.Increment(ref kept ? ref recorder._kept : ref recorder._discarded);
                return kept;
            }
        }
    }

    public sealed class BatchGates
    {
        private readonly Dictionary<string, TaskCompletionSource> _entered = new()
        {
            ["a"] = Create(),
            ["b"] = Create()
        };

        public TaskCompletionSource Release { get; } = Create();

        public TaskCompletionSource Entered(string value) => _entered[value];

        private static TaskCompletionSource Create()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class BatchQuery
    {
        public string GetEcho(string value) => value;

        public async Task<string> GetWaitAsync(
            string value,
            [Service] BatchGates gates,
            CancellationToken cancellationToken)
        {
            gates.Entered(value).TrySetResult();
            await gates.Release.Task.WaitAsync(cancellationToken);
            return value;
        }

        public async Task<string> GetSlowAsync(string value, [Service] BatchGates gates)
        {
            gates.Entered(value).TrySetResult();
            await gates.Release.Task;
            return value;
        }
    }
}

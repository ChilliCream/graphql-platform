using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferContextPoolingTests
{
    [Fact]
    public async Task Execute_Should_ReturnFullData_When_AbortedDeferredResolverFinishesDuringNextRequest()
    {
        // arrange
        var gates = new PoolingGates();
        var executor = await CreateExecutorAsync(gates);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abort = new CancellationTokenSource();
        var unobserved = new UnobservedExceptionCollector();

        await using var deferred = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ a ... @defer { slow } }").Build(),
            abort.Token);
        var deferredStream = deferred.ExpectResponseStream();
        var deferredResults = deferredStream.ReadResultsAsync().GetAsyncEnumerator(timeout.Token);
        await deferredResults.MoveNextAsync();
        await deferredResults.Current.DisposeAsync();
        await gates.SlowEntered.Task.WaitAsync(timeout.Token);

        // act
        await abort.CancelAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);

        var next = executor.ExecuteAsync("{ a b }", timeout.Token);
        await gates.BEntered.Task.WaitAsync(timeout.Token);
        gates.SlowGate.SetResult();
        await gates.SlowReturned.Task.WaitAsync(timeout.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);
        var nextCompletedEarly = next.IsCompleted;
        gates.BGate.SetResult();
        await using var nextResult = await next.WaitAsync(timeout.Token);

        var unobservedExceptions = unobserved.Collect();

        // assert
        Assert.False(nextCompletedEarly);
        nextResult.ExpectOperationResult().ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": "a",
                "b": "b"
              }
            }
            """);
        Assert.Empty(unobservedExceptions);
    }

    [Fact]
    public async Task Execute_Should_ReturnNoErrors_When_AbortedDeferredResolverFinishesBeforeNextRequest()
    {
        // arrange
        var gates = new PoolingGates { SlowReturnsNull = true };
        var executor = await CreateExecutorAsync(gates);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var abort = new CancellationTokenSource();
        var unobserved = new UnobservedExceptionCollector();

        await using var deferred = await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ a ... @defer { slow } }").Build(),
            abort.Token);
        var deferredStream = deferred.ExpectResponseStream();
        var deferredResults = deferredStream.ReadResultsAsync().GetAsyncEnumerator(timeout.Token);
        await deferredResults.MoveNextAsync();
        await deferredResults.Current.DisposeAsync();
        await gates.SlowEntered.Task.WaitAsync(timeout.Token);

        // act
        await abort.CancelAsync();
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);

        gates.SlowGate.SetResult();
        await gates.SlowReturned.Task.WaitAsync(timeout.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);

        await using var nextResult = await executor.ExecuteAsync("{ a }", timeout.Token);

        var unobservedExceptions = unobserved.Collect();

        // assert
        nextResult.ExpectOperationResult().ToJson().MatchInlineSnapshot(
            """
            {
              "data": {
                "a": "a"
              }
            }
            """);
        Assert.Empty(unobservedExceptions);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(PoolingGates gates)
        => new ServiceCollection()
            .AddSingleton(gates)
            .AddGraphQL()
            .AddQueryType<PoolingQuery>()
            .ModifyOptions(o => o.EnableDefer = true)
            .BuildRequestExecutorAsync();

    private sealed class UnobservedExceptionCollector
    {
        private readonly List<string> _exceptions = [];

        public UnobservedExceptionCollector()
        {
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        }

        public IReadOnlyList<string> Collect()
        {
            // unobserved task exceptions surface when the faulted task is finalized.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;

            lock (_exceptions)
            {
                return [.. _exceptions];
            }
        }

        private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
        {
            var exception = args.Exception.ToString();

            if (exception.Contains("HotChocolate.Execution.Processing.Tasks.", StringComparison.Ordinal))
            {
                lock (_exceptions)
                {
                    _exceptions.Add(exception);
                }
            }
        }
    }

    public sealed class PoolingGates
    {
        public bool SlowReturnsNull { get; init; }

        public TaskCompletionSource SlowEntered { get; } = Create();

        public TaskCompletionSource SlowGate { get; } = Create();

        public TaskCompletionSource SlowReturned { get; } = Create();

        public TaskCompletionSource BEntered { get; } = Create();

        public TaskCompletionSource BGate { get; } = Create();

        private static TaskCompletionSource Create()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class PoolingQuery
    {
        public string GetA() => "a";

        public async Task<string> GetBAsync([Service] PoolingGates gates)
        {
            gates.BEntered.TrySetResult();
            await gates.BGate.Task;
            return "b";
        }

        public async Task<string> GetSlowAsync([Service] PoolingGates gates)
        {
            gates.SlowEntered.TrySetResult();
            await gates.SlowGate.Task;
            gates.SlowReturned.TrySetResult();
            return gates.SlowReturnsNull ? null! : "slow-value";
        }
    }
}

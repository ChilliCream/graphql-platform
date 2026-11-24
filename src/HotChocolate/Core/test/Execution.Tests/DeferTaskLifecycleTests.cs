using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class DeferTaskLifecycleTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task DeferTask_Should_Complete_When_ResponseStreamIsDisposed_BeforePayloadCompletes()
    {
        // arrange
        var resolverStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeDeferredResolver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deferTaskCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new DeferTaskCompletionListener(deferTaskCompleted);
        var executor = await CreateExecutorAsync(resolverStarted, completeDeferredResolver, listener);
        var result = await executor.ExecuteAsync(
                "{ root ... @defer { deferred } }",
                TestContext.Current.CancellationToken)
            .WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var stream = Assert.IsType<ResponseStream>(result);

        // act
        try
        {
            await resolverStarted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await stream.DisposeAsync();
            completeDeferredResolver.TrySetResult();
            await deferTaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            completeDeferredResolver.TrySetResult();
            await stream.DisposeAsync();
        }

        // assert
        Assert.Equal(1, listener.CompletedCount);
    }

    [Fact]
    public async Task ReadResultsAsync_Should_Complete_When_RequestTokenIsAlreadyCancelled()
    {
        // arrange
        var resolverStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var completeDeferredResolver = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deferTaskCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = new DeferTaskCompletionListener(deferTaskCompleted);
        var executor = await CreateExecutorAsync(resolverStarted, completeDeferredResolver, listener);
        using var requestCancellation = new CancellationTokenSource();
        var result = await executor.ExecuteAsync(
            "{ root ... @defer { deferred } }",
            requestCancellation.Token);
        var stream = Assert.IsType<ResponseStream>(result);
        IAsyncEnumerator<OperationResult>? enumerator = null;
        Task<bool>? moveNext = null;
        var hasResult = true;

        // act
        try
        {
            await resolverStarted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            requestCancellation.Cancel();
            enumerator = stream.ReadResultsAsync().GetAsyncEnumerator(TestContext.Current.CancellationToken);
            moveNext = enumerator.MoveNextAsync().AsTask();
            hasResult = await moveNext.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }
        finally
        {
            completeDeferredResolver.TrySetResult();

            if (moveNext is not null)
            {
                await moveNext;
            }

            if (enumerator is not null)
            {
                await enumerator.DisposeAsync();
            }

            await deferTaskCompleted.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
            await stream.DisposeAsync();
        }

        // assert
        Assert.False(hasResult);
        Assert.Equal(1, listener.CompletedCount);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(
        TaskCompletionSource resolverStarted,
        TaskCompletionSource completeDeferredResolver,
        DeferTaskCompletionListener listener)
        => new ServiceCollection()
            .AddGraphQL()
            .AddDiagnosticEventListener(_ => listener)
            .AddQueryType(
                descriptor =>
                {
                    descriptor.Field("root").Type<StringType>().Resolve("root");
                    descriptor
                        .Field("deferred")
                        .Type<StringType>()
                        .Resolve(async _ =>
                        {
                            resolverStarted.TrySetResult();
                            await completeDeferredResolver.Task.ConfigureAwait(false);
                            return "deferred";
                        });
                })
            .ModifyOptions(options => options.EnableDefer = true)
            .BuildRequestExecutorAsync();

    private sealed class DeferTaskCompletionListener(TaskCompletionSource completed) : ExecutionDiagnosticEventListener
    {
        private int _completedCount;

        public int CompletedCount => Volatile.Read(ref _completedCount);

        public override bool EnableResolveFieldValue => true;

        public override IDisposable RunTask(IExecutionTask task)
            => task.IsDeferred && task.BranchId == BranchTracker.SystemBranchId
                ? new CompletionScope(completed, this)
                : EmptyScope;

        private sealed class CompletionScope(TaskCompletionSource completed, DeferTaskCompletionListener listener)
            : IDisposable
        {
            public void Dispose()
            {
                Interlocked.Increment(ref listener._completedCount);
                completed.TrySetResult();
            }
        }
    }
}

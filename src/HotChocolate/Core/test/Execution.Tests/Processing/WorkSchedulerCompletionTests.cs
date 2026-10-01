#if DEBUG
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Processing;

public class WorkSchedulerCompletionTests
{
    [Fact]
    public async Task ExecuteAsync_Should_WaitForCompletionBookkeeping_When_LastResolverStopsRunning()
    {
        // arrange
        var entered = new TaskCompletionSource<WorkScheduler>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var pausedAgain = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resolverGate = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var cancellationToken = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        services.AddSingleton(resolverGate);
        var executor = await services
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddDiagnosticEventListener(_ => new CompletionHookListener(
                stopped,
                resumed,
                pausedAgain,
                scheduler =>
                {
                    entered.TrySetResult(scheduler);
                    release.Wait(TimeSpan.FromSeconds(10), cancellationToken);
                }))
            .BuildRequestExecutorAsync(cancellationToken: cancellationToken);

        // act
        var execution = executor.ExecuteAsync("{ value }", cancellationToken);
        try
        {
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            resolverGate.SetResult(42);
            var scheduler = await entered.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            scheduler.OnNext(new BatchDispatchEventArgs(BatchDispatchEventType.Enqueued));
            await resumed.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            var first = await Task.WhenAny(
                execution,
                pausedAgain.Task,
                Task.Delay(TimeSpan.FromSeconds(1), cancellationToken));

            // assert
            Assert.NotSame(execution, first);
        }
        finally
        {
            release.Set();
            await using var result = await execution.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }

    public sealed class Query
    {
        public Task<int> GetValueAsync([Service] TaskCompletionSource<int> resolverGate)
            => resolverGate.Task;
    }

    private sealed class CompletionHookListener(
        TaskCompletionSource stopped,
        TaskCompletionSource resumed,
        TaskCompletionSource pausedAgain,
        Action<WorkScheduler> hook)
        : ExecutionDiagnosticEventListener
    {
        private int _processingPasses;

        public override IDisposable ExecuteRequest(RequestContext context)
        {
            context.ContextData[WorkScheduler.LastResolverCompletionHookKey] = hook;
            return EmptyScope;
        }

        public override void StartProcessing(RequestContext context)
        {
            if (Interlocked.Increment(ref _processingPasses) == 2)
            {
                resumed.TrySetResult();
            }
        }

        public override void StopProcessing(RequestContext context)
        {
            if (_processingPasses == 1)
            {
                stopped.TrySetResult();
            }
            else
            {
                pausedAgain.TrySetResult();
            }
        }
    }
}
#endif

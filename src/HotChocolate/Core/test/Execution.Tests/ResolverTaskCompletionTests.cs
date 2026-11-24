using System.Runtime.CompilerServices;
using GreenDonut;
using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution;

public class ResolverTaskCompletionTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task Execute_Should_ReturnResolverTaskToItsPool_When_ContextReturnsAsCancelledTaskCompletes()
    {
        // arrange
        var gate = new ResolverGate();
        var dispatcher = new FailOnCancelBatchDispatcher();
        var resolverTasks = new CountingResolverTaskPool();
        var contexts = new LastReleaseContextPool();
        var executor = await CreateExecutorAsync(gate, dispatcher, resolverTasks, contexts);

        // act
        await ExecuteCancelledAsync(executor, gate, dispatcher, "{ gated }");
        gate.Open();
        await contexts.WaitForReturnAsync();

        // assert
        await resolverTasks.WaitUntilAllReturnedAsync();
        Assert.True(contexts.WasReturnedOnTaskRelease);
    }

    [Fact]
    public async Task Execute_Should_ReturnBatchChildTasksToTheirPool_When_ContextReturnsAsCancelledTaskCompletes()
    {
        // arrange
        var gate = new ResolverGate();
        var dispatcher = new FailOnCancelBatchDispatcher();
        var resolverTasks = new CountingResolverTaskPool();
        var contexts = new LastReleaseContextPool();
        var executor = await CreateExecutorAsync(gate, dispatcher, resolverTasks, contexts);

        // act
        await ExecuteCancelledAsync(executor, gate, dispatcher, "{ holder { batchGated } }");
        gate.Open();
        await contexts.WaitForReturnAsync();

        // assert
        await resolverTasks.WaitUntilAllReturnedAsync();
        Assert.True(contexts.WasReturnedOnTaskRelease);
    }

    private static async Task ExecuteCancelledAsync(
        IRequestExecutor executor,
        ResolverGate gate,
        FailOnCancelBatchDispatcher dispatcher,
        string query)
    {
        using var abort = new CancellationTokenSource();
        var execution = Task.Run(
            async () =>
            {
                try
                {
                    var result = await executor.ExecuteAsync(query, abort.Token);
                    await result.DisposeAsync();
                }
                catch (InvalidOperationException)
                {
                    // the dispatcher failure ends the request while the resolver still runs.
                }
            },
            TestContext.Current.CancellationToken);

        await gate.Entered.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        await abort.CancelAsync();

        // the dispatcher fails on the cancelled request, so the scheduler loop ends while the resolver runs.
        dispatcher.ReportEnqueuedBatch();
        await execution.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
    }

    private static ValueTask<IRequestExecutor> CreateExecutorAsync(
        ResolverGate gate,
        FailOnCancelBatchDispatcher dispatcher,
        CountingResolverTaskPool resolverTasks,
        LastReleaseContextPool contexts)
    {
        var builder = new ServiceCollection()
            .AddSingleton(gate)
            .AddSingleton<ObjectPool<ResolverTask>>(resolverTasks)
            .AddGraphQL()
            .AddQueryType(
                descriptor =>
                {
                    descriptor
                        .Field("gated")
                        .Type<StringType>()
                        .Resolve(async context =>
                        {
                            await context.Service<ResolverGate>().WaitAsync();
                            return "gated";
                        });
                    descriptor
                        .Field("holder")
                        .Type("Holder")
                        .Resolve(new object());
                })
            .AddObjectType(
                descriptor =>
                {
                    descriptor.Name("Holder");
                    descriptor
                        .Field("batchGated")
                        .Type<StringType>()
                        .ResolveBatch(ResolveBatchGatedAsync);
                });

        builder.Services.RemoveAll<IBatchDispatcher>();
        builder.Services.AddScoped<IBatchDispatcher>(_ => dispatcher);
        builder.Services.AddSingleton<IFactory<OperationContextOwner>>(
            sp => new OperationContextOwnerFactory(
                contexts.Wrap(sp.GetRequiredService<ObjectPool<OperationContext>>())));

        return builder.BuildRequestExecutorAsync();
    }

    private static async ValueTask<IReadOnlyList<ResolverResult>> ResolveBatchGatedAsync(
        IReadOnlyList<IResolverContext> contexts)
    {
        await contexts[0].Service<ResolverGate>().WaitAsync();

        var results = new ResolverResult[contexts.Count];

        for (var i = 0; i < contexts.Count; i++)
        {
            results[i] = ResolverResult.Ok("gated");
        }

        return results;
    }

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_idleSignal")]
    private static extern ref TaskCompletionSource? IdleSignal(OperationContext context);

    public sealed class ResolverGate
    {
        private readonly TaskCompletionSource _open = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitAsync()
        {
            Entered.TrySetResult();

            // the resolver ignores the request cancellation, so it is still running when the request ends.
            await _open.Task.ConfigureAwait(false);
        }

        public void Open() => _open.TrySetResult();
    }

    private sealed class FailOnCancelBatchDispatcher : IBatchDispatcher
    {
        private readonly List<IObserver<BatchDispatchEventArgs>> _observers = [];

        public void ReportEnqueuedBatch()
        {
            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(new BatchDispatchEventArgs(BatchDispatchEventType.Enqueued));
            }
        }

        public void Schedule(Batch batch)
        {
        }

        public IDisposable Subscribe(IObserver<BatchDispatchEventArgs> observer)
        {
            _observers.Add(observer);
            return new Subscription();
        }

        public void BeginDispatch(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new InvalidOperationException("The dispatcher failed after the request was cancelled.");
            }
        }

        public void Dispose()
        {
        }

        private sealed class Subscription : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }

    private sealed class LastReleaseContextPool
    {
        private readonly TaskCompletionSource _returned = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool WasReturnedOnTaskRelease { get; private set; }

        public ObjectPool<OperationContext> Wrap(ObjectPool<OperationContext> inner) => new Pool(this, inner);

        public Task WaitForReturnAsync() => _returned.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);

        private sealed class Pool(LastReleaseContextPool owner, ObjectPool<OperationContext> inner)
            : ObjectPool<OperationContext>
        {
            public override OperationContext Get() => inner.Get();

            public override void Return(OperationContext obj)
            {
                if (!obj.HasOutstandingTasks)
                {
                    inner.Return(obj);
                    owner._returned.TrySetResult();
                    return;
                }

                // the context goes back to the pool on the thread of the task that releases it last,
                // at the moment that task releases it.
                var signal = new TaskCompletionSource();
                signal.Task.ContinueWith(
                    _ =>
                    {
                        owner.WasReturnedOnTaskRelease = true;
                        inner.Return(obj);
                        owner._returned.TrySetResult();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
                IdleSignal(obj) = signal;
            }
        }
    }

    private sealed class CountingResolverTaskPool : ObjectPool<ResolverTask>
    {
        private readonly SemaphoreSlim _returned = new(0);
        private int _outstanding;

        public override ResolverTask Get()
        {
            Interlocked.Increment(ref _outstanding);
            return new ResolverTask(this);
        }

        public override void Return(ResolverTask obj)
        {
            if (Interlocked.Decrement(ref _outstanding) == 0)
            {
                _returned.Release();
            }
        }

        public async Task WaitUntilAllReturnedAsync()
        {
            while (Volatile.Read(ref _outstanding) != 0)
            {
                if (!await _returned.WaitAsync(s_timeout, TestContext.Current.CancellationToken))
                {
                    throw new TimeoutException("A resolver task was not returned to its pool in time.");
                }
            }
        }
    }
}

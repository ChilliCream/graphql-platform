using System.Collections.Immutable;
using System.Diagnostics;
using GreenDonut;

namespace HotChocolate.Fetching;

public class BatchDispatcherTests
{
    [Fact]
    public async Task BeginDispatch_Evaluates_And_Dispatches_Enqueued_Batch()
    {
        // arrange
        var observer = new TestObserver();
        var scheduler = new BatchDispatcher(new DataLoaderDiagnosticEventListener());
        using var session = scheduler.Subscribe(observer);
        scheduler.Schedule(new TestBatch());
        Assert.Equal(BatchDispatchEventType.Enqueued, observer.Events[0]);

        // act
        scheduler.BeginDispatch();

        // assert
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(10);

            if (observer.Events.Count >= 5)
            {
                break;
            }
        }

        scheduler.Dispose();

        await Task.Delay(10);

        Assert.Collection(
            observer.Events.Take(5),
            t => Assert.Equal(BatchDispatchEventType.Enqueued, t),
            t => Assert.Equal(BatchDispatchEventType.CoordinatorStarted, t),
            t => Assert.Equal(BatchDispatchEventType.Evaluated, t),
            t => Assert.Equal(BatchDispatchEventType.Dispatched, t),
            t => Assert.Equal(BatchDispatchEventType.CoordinatorCompleted, t));
    }

    [Fact]
    public async Task Initialize_Nothing_ShouldMatchSnapshot()
    {
        // arrange
        var observer = new TestObserver();
        var scheduler = new BatchDispatcher(new DataLoaderDiagnosticEventListener());
        using var session = scheduler.Subscribe(observer);

        // act
        scheduler.Schedule(new TestBatch());

        // assert
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(10);

            if (observer.Events.Count >= 3)
            {
                break;
            }
        }

        Assert.Collection(
            observer.Events,
            t => Assert.Equal(BatchDispatchEventType.Enqueued, t));
    }

    [Fact]
    public void Schedule_OneAction_ShouldRaiseTaskEnqueued()
    {
        // arrange
        var observer = new TestObserver();
        var scheduler = new BatchDispatcher(new DataLoaderDiagnosticEventListener());

        // act
        using var session = scheduler.Subscribe(observer);
        scheduler.Schedule(new TestBatch());

        // assert
        Assert.Equal(BatchDispatchEventType.Enqueued, observer.Events[0]);
    }

    [Fact]
    public async Task BeginDispatch_Allows_Nested_Batch_To_Dispatch_While_Parent_Is_InFlight()
    {
        // arrange
        var observer = new TestObserver();
        var scheduler = new BatchDispatcher(new DataLoaderDiagnosticEventListener());
        using var session = scheduler.Subscribe(observer);

        var innerDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var outerCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var innerBatch = new DelegatingBatch(
            () =>
            {
                innerDispatched.TrySetResult();
                return Task.CompletedTask;
            });

        var outerBatch = new DelegatingBatch(
            async () =>
            {
                scheduler.Schedule(innerBatch);
                scheduler.BeginDispatch();
                await innerDispatched.Task;
                outerCompleted.TrySetResult();
            });

        scheduler.Schedule(outerBatch);

        // act
        scheduler.BeginDispatch();
        var completedTask = await Task.WhenAny(outerCompleted.Task, Task.Delay(3_000));

        // assert
        scheduler.Dispose();
        Assert.Same(outerCompleted.Task, completedTask);
        await outerCompleted.Task;
    }

    [Fact]
    public async Task BeginDispatch_Many_Concurrent_Nested_Batches_Do_Not_Deadlock()
    {
        // arrange — N outer batches each schedule and await a nested inner batch.
        // Previously this would deadlock when N >= MaxParallelBatches because
        // the capacity limit prevented nested batches from being dispatched
        // while all slots were occupied by their parents.
        const int batchCount = 8;
        var scheduler = new BatchDispatcher(new DataLoaderDiagnosticEventListener());
        var observer = new TestObserver();
        using var session = scheduler.Subscribe(observer);

        var allCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var remaining = batchCount;

        for (var i = 0; i < batchCount; i++)
        {
            var innerDispatched = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var innerBatch = new DelegatingBatch(
                () =>
                {
                    innerDispatched.TrySetResult();
                    return Task.CompletedTask;
                });

            var outerBatch = new DelegatingBatch(
                async () =>
                {
                    scheduler.Schedule(innerBatch);
                    scheduler.BeginDispatch();
                    await innerDispatched.Task;

                    if (Interlocked.Decrement(ref remaining) == 0)
                    {
                        allCompleted.TrySetResult();
                    }
                });

            scheduler.Schedule(outerBatch);
        }

        // act
        scheduler.BeginDispatch();
        var completedTask = await Task.WhenAny(allCompleted.Task, Task.Delay(5_000));

        // assert
        scheduler.Dispose();
        Assert.Same(allCompleted.Task, completedTask);
        await allCompleted.Task;
    }

    public class TestObserver : IObserver<BatchDispatchEventArgs>
    {
        public ImmutableList<BatchDispatchEventType> Events { get; private set; } = [];

        public void OnNext(BatchDispatchEventArgs value)
        {
            Events = Events.Add(value.Type);
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    public class TestBatch : Batch
    {
        private BatchStatus _status = BatchStatus.Enqueued;

        public override int Size => 1;

        public override BatchStatus Status => _status;

        public override long ModifiedTimestamp { get; } = Stopwatch.GetTimestamp();

        public override long CreatedTimestamp { get; } = Stopwatch.GetTimestamp();

        public override bool Touch()
        {
            if (_status is BatchStatus.Touched)
            {
                return true;
            }

            _status = BatchStatus.Touched;
            return false;
        }

        public override Task DispatchAsync()
            => Task.CompletedTask;
    }

    public class DelegatingBatch : Batch
    {
        private readonly Func<Task> _dispatch;
        private BatchStatus _status = BatchStatus.Enqueued;

        public DelegatingBatch(Func<Task> dispatch)
        {
            _dispatch = dispatch;
        }

        public override int Size => 1;

        public override BatchStatus Status => _status;

        public override long ModifiedTimestamp { get; } = Stopwatch.GetTimestamp();

        public override long CreatedTimestamp { get; } = Stopwatch.GetTimestamp();

        public override bool Touch()
        {
            if (_status is BatchStatus.Touched)
            {
                return true;
            }

            _status = BatchStatus.Touched;
            return false;
        }

        public override Task DispatchAsync() => _dispatch();
    }
}

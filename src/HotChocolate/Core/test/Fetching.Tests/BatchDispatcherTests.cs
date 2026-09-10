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
        scheduler.BeginDispatch(TestContext.Current.CancellationToken);

        // assert
        for (var i = 0; i < 10; i++)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);

            if (observer.Events.Count >= 5)
            {
                break;
            }
        }

        scheduler.Dispose();

        await Task.Delay(10, TestContext.Current.CancellationToken);

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
            await Task.Delay(10, TestContext.Current.CancellationToken);

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
        scheduler.BeginDispatch(TestContext.Current.CancellationToken);
        var completedTask = await Task.WhenAny(
            outerCompleted.Task,
            Task.Delay(3_000, TestContext.Current.CancellationToken));

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
        scheduler.BeginDispatch(TestContext.Current.CancellationToken);
        var completedTask = await Task.WhenAny(
            allCompleted.Task,
            Task.Delay(5_000, TestContext.Current.CancellationToken));

        // assert
        scheduler.Dispose();
        Assert.Same(allCompleted.Task, completedTask);
        await allCompleted.Task;
    }

    [Fact]
    public async Task BeginDispatch_Should_IncludeLateArrivingItems_When_BatchIsStillFilling()
    {
        // arrange
        var options = new BatchDispatcherOptions { BatchSettleTimeUs = 25_000 };
        var dispatcher = new BatchDispatcher(new DataLoaderDiagnosticEventListener(), options);
        var batch = new CollectingBatch();
        dispatcher.Schedule(batch);
        dispatcher.BeginDispatch(TestContext.Current.CancellationToken);

        // act
        // add items spaced roughly 1000 microseconds apart using a tight stopwatch spin.
        // Task.Delay cannot be used here because its granularity is milliseconds.
        for (var i = 0; i < 7; i++)
        {
            var start = Stopwatch.GetTimestamp();

            while (Stopwatch.GetElapsedTime(start).TotalMicroseconds < 1_000)
            {
            }

            batch.AddItem();
        }

        var completed = await Task.WhenAny(
            batch.Dispatched,
            Task.Delay(5_000, TestContext.Current.CancellationToken));

        // assert
        dispatcher.Dispose();
        Assert.Same(batch.Dispatched, completed);
        Assert.Equal(8, batch.DispatchedSize);
    }

    [Fact]
    public async Task BeginDispatch_Should_NotForceDispatch_When_MaxBatchWaitTimeIsExplicitlyZero()
    {
        // arrange
        var observer = new TestObserver();
        var options = new BatchDispatcherOptions { MaxBatchWaitTimeUs = 0 };
        var dispatcher = new BatchDispatcher(new DataLoaderDiagnosticEventListener(), options);
        using var session = dispatcher.Subscribe(observer);
        var batch = new ContinuouslyModifiedBatch();
        dispatcher.Schedule(batch);
        dispatcher.BeginDispatch(TestContext.Current.CancellationToken);

        // act
        await Task.Delay(150, TestContext.Current.CancellationToken);

        // assert
        // the batch must have been evaluated but deliberately never dispatched
        dispatcher.Dispose();
        Assert.Contains(BatchDispatchEventType.Evaluated, observer.Events);
        Assert.False(batch.Dispatched.IsCompleted);
    }

    [Fact]
    public async Task BeginDispatch_Should_ForceDispatch_When_BatchNeverSettles()
    {
        // arrange
        var dispatcher = new BatchDispatcher(new DataLoaderDiagnosticEventListener());
        var batch = new ContinuouslyModifiedBatch();
        dispatcher.Schedule(batch);
        dispatcher.BeginDispatch(TestContext.Current.CancellationToken);

        // act
        var completed = await Task.WhenAny(
            batch.Dispatched,
            Task.Delay(5_000, TestContext.Current.CancellationToken));

        // assert
        dispatcher.Dispose();
        Assert.Same(batch.Dispatched, completed);
    }

    [Fact]
    public void MaxBatchWaitTimeUs_Should_ReturnDefault_When_NotSet()
    {
        // arrange
        var defaultOptions = default(BatchDispatcherOptions);
        var explicitOptions = new BatchDispatcherOptions { MaxBatchWaitTimeUs = 0 };

        // act
        var defaultValue = defaultOptions.MaxBatchWaitTimeUs;
        var explicitValue = explicitOptions.MaxBatchWaitTimeUs;

        // assert
        Assert.Equal(50_000, defaultValue);
        Assert.Equal(0, explicitValue);
    }

    [Fact]
    public void BatchSettleTimeUs_Should_ReturnDefault_When_NotSet()
    {
        // arrange
        var defaultOptions = default(BatchDispatcherOptions);
        var explicitOptions = new BatchDispatcherOptions { BatchSettleTimeUs = 1_000 };

        // act
        var defaultValue = defaultOptions.BatchSettleTimeUs;
        var explicitValue = explicitOptions.BatchSettleTimeUs;

        // assert
        Assert.Equal(250, defaultValue);
        Assert.Equal(1_000, explicitValue);
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

    public class CollectingBatch : Batch
    {
        private readonly TaskCompletionSource _dispatched =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private volatile BatchStatus _status = BatchStatus.Enqueued;
        private volatile int _size = 1;
        private long _modifiedTimestamp = Stopwatch.GetTimestamp();
        private readonly long _createdTimestamp = Stopwatch.GetTimestamp();

        public Task Dispatched => _dispatched.Task;

        public int DispatchedSize { get; private set; }

        public override int Size => _size;

        public override BatchStatus Status => _status;

        public override long ModifiedTimestamp => _modifiedTimestamp;

        public override long CreatedTimestamp => _createdTimestamp;

        public void AddItem()
        {
            Interlocked.Increment(ref _size);
            _modifiedTimestamp = Stopwatch.GetTimestamp();
            _status = BatchStatus.Enqueued;
        }

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
        {
            DispatchedSize = Size;
            _dispatched.TrySetResult();
            return Task.CompletedTask;
        }
    }

    public class ContinuouslyModifiedBatch : Batch
    {
        private readonly TaskCompletionSource _dispatched =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly long _createdTimestamp = Stopwatch.GetTimestamp();

        public Task Dispatched => _dispatched.Task;

        public override int Size => 1;

        public override BatchStatus Status => BatchStatus.Enqueued;

        public override long ModifiedTimestamp => Stopwatch.GetTimestamp();

        public override long CreatedTimestamp => _createdTimestamp;

        public override bool Touch() => false;

        public override Task DispatchAsync()
        {
            _dispatched.TrySetResult();
            return Task.CompletedTask;
        }
    }
}

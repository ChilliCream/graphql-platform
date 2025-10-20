using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using GreenDonut;

namespace HotChocolate.Fetching;

public class BatchDispatcherTests
{
    [Fact]
    public async Task BeginDispatch_Evaluates_And_Dispatches_Enqueued_Batch()
    {
        // arrange
        var observer = new TestObserver();
        var scheduler = new BatchDispatcher();
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
        var scheduler = new BatchDispatcher();
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
        var scheduler = new BatchDispatcher();

        // act
        using var session = scheduler.Subscribe(observer);
        scheduler.Schedule(new TestBatch());

        // assert
        Assert.Equal(BatchDispatchEventType.Enqueued, observer.Events[0]);
    }

    [Fact]
    public async Task ThreadPoolHeadroom_WhenPendingWork_ShouldReturnFalse()
    {
        // arrange
        var method = typeof(BatchDispatcher)
            .GetMethod(
                "ThreadPoolHasHeadroom",
                BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not locate headroom probe.");

        var blockerCount = Math.Max(2, Environment.ProcessorCount);
        using var gate = new ManualResetEventSlim(false);
        using var started = new CountdownEvent(blockerCount);

        var blockers = new Task[blockerCount];
        for (var i = 0; i < blockerCount; i++)
        {
            blockers[i] = Task.Run(() =>
            {
                started.Signal();
                gate.Wait();
            });
        }

        Assert.True(started.Wait(TimeSpan.FromSeconds(10)), "Blocking tasks failed to start.");

        var extraTasks = new Task[blockerCount * 8];
        for (var i = 0; i < extraTasks.Length; i++)
        {
            extraTasks[i] = Task.Run(() => gate.Wait());
        }

        try
        {
            // act
            var pending = await WaitForPendingWorkItemsAsync(TimeSpan.FromSeconds(5));

            // assert
            Assert.True(pending > 0, "Expected pending work items for headroom regression test.");

            var hasHeadroom = (bool)method.Invoke(null, null)!;
            Assert.False(hasHeadroom);
        }
        finally
        {
            gate.Set();

            var allTasks = new Task[blockers.Length + extraTasks.Length];
            Array.Copy(blockers, 0, allTasks, 0, blockers.Length);
            Array.Copy(extraTasks, 0, allTasks, blockers.Length, extraTasks.Length);
            await Task.WhenAll(allTasks);
        }
    }

    private static async Task<long> WaitForPendingWorkItemsAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            var pending = ThreadPool.PendingWorkItemCount;

            if (pending > 0)
            {
                return pending;
            }

            await Task.Yield();
        }

        return ThreadPool.PendingWorkItemCount;
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
}

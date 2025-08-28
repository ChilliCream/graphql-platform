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

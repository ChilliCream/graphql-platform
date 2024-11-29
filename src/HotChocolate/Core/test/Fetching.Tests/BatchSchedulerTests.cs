using HotChocolate.Execution;
using Moq;

namespace HotChocolate.Fetching;

public class BatchSchedulerTests
{
    [Fact]
    public void Dispatch_OneAction_ShouldDispatchOneAction()
    {
        // arrange
        var context = new Mock<IExecutionTaskContext>();
        context.Setup(t => t.Register(It.IsAny<IExecutionTask>()));
        var hasTask = false;

        var scheduler = new BatchScheduler();
        scheduler.TaskEnqueued += (_, _) => hasTask = true;

        ValueTask Dispatch() => default;

        scheduler.Schedule(Dispatch);
        Assert.True(hasTask);
        hasTask = false;

        // act
        scheduler.BeginDispatch();

        // assert
        Assert.False(hasTask);
    }

    [Fact]
    public void Initialize_Nothing_ShouldMatchSnapshot()
    {
        // act
        var scheduler = new BatchScheduler();

        // assert
        scheduler.MatchSnapshot();
    }

    [Fact]
    public void Schedule_OneAction_HasTasksShouldReturnTrue()
    {
        // arrange
        var hasTask = false;
        var scheduler = new BatchScheduler();
        scheduler.TaskEnqueued += (_, _) => hasTask = true;
        ValueTask Dispatch() => default;

        // act
        scheduler.Schedule(Dispatch);

        // assert
        Assert.True(hasTask);
    }

    [Fact]
    public void Schedule_OneAction_ShouldRaiseTaskEnqueued()
    {
        // arrange
        var hasBeenRaised = false;
        var scheduler = new BatchScheduler();
        ValueTask Dispatch() => default;

        scheduler.TaskEnqueued += (_, _) => hasBeenRaised = true;

        // act
        scheduler.Schedule(Dispatch);

        // assert
        Assert.True(hasBeenRaised);
    }
}

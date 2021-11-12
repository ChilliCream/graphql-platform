using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Fetching;
using Moq;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate;

public class BatchSchedulerTests
{
    [Fact]
    public Task Dispatch_OneAction_ShouldDispatchOneAction()
    {
        // arrange
        var context = new Mock<IExecutionTaskContext>();
        context.Setup(t => t.Register(It.IsAny<IExecutionTask>()));

        var scheduler = new BatchScheduler();

        ValueTask Dispatch() => default;

        scheduler.Schedule(Dispatch);
        Assert.True(scheduler.HasTasks);

        // act
        scheduler.BeginDispatch();

        // assert
        Assert.False(scheduler.HasTasks);
        return Task.CompletedTask;
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
        var scheduler = new BatchScheduler();
        ValueTask Dispatch() => default;

        // act
        scheduler.Schedule(Dispatch);

        // assert
        Assert.True(scheduler.HasTasks);
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

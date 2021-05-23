using System;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class BatchSchedulerTests
    {
        [Fact]
        public void Dispatch_OneAction_ShouldDispatchOneAction()
        {
            // arrange
            var scheduler = new BatchScheduler();
            ValueTask Dispatch() => default;

            scheduler.Schedule(Dispatch);
            Assert.True(scheduler.HasTasks);

            // act
            scheduler.Dispatch(_ => { });

            // assert
            Assert.True(scheduler.HasTasks);
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
}

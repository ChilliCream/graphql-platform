using System;
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
            var hasBeenDispatched = false;
            var scheduler = new BatchScheduler();
            Action dispatch = () =>
            {
                hasBeenDispatched = true;
            };

            scheduler.Schedule(dispatch);

            // act
            scheduler.Dispatch();

            // assert
            Assert.True(hasBeenDispatched);
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
            Action dispatch = () => { };

            // act
            scheduler.Schedule(dispatch);

            // assert
            Assert.True(scheduler.HasTasks);
        }

        [Fact]
        public void Schedule_OneAction_ShouldRaiseTaskEnqueued()
        {
            // arrange
            var hasBeenRaised = false;
            var scheduler = new BatchScheduler();
            Action dispatch = () => { };

            scheduler.TaskEnqueued += (s, e) =>
            {
                hasBeenRaised = true;
            };

            // act
            scheduler.Schedule(dispatch);

            // assert
            Assert.True(hasBeenRaised);
        }
    }
}

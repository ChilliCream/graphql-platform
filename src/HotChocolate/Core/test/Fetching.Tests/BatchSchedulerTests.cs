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
            Func<ValueTask> dispatch = () => default;

            scheduler.Schedule(dispatch);
            Assert.True(scheduler.HasTasks);

            // act
            scheduler.Dispatch(d => { });

            // assert
            Assert.False(scheduler.HasTasks);
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
            Func<ValueTask> dispatch = () => default;

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
            Func<ValueTask> dispatch = () => default;

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

using System;
using HotChocolate.Fetching;
using Xunit;

namespace HotChocolate
{
    public class AutoBatchSchedulerTests
    {
        [Fact]
        public void Schedule_OneAction_DispatchesImmediately()
        {
            // arrange
            var hasBeenDispatched = false;
            var scheduler = new AutoBatchScheduler();
            Action dispatch = () =>
            {
                hasBeenDispatched = true;
            };

            // act
            scheduler.Schedule(dispatch);

            // assert
            Assert.True(hasBeenDispatched);
        }
    }
}

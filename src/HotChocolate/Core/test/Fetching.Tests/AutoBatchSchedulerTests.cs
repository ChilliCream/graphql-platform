using System;
using System.Threading.Tasks;
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
            Func<ValueTask> dispatch = () =>
            {
                hasBeenDispatched = true;
                return default;
            };

            // act
            scheduler.Schedule(dispatch);

            // assert
            Assert.True(hasBeenDispatched);
        }
    }
}

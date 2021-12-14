using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;
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
            var wait = new AutoResetEvent(false);

            ValueTask Dispatch()
            {
                hasBeenDispatched = true;
                wait.Set();
                return default;
            }

            // act
            scheduler.Schedule(Dispatch);

            // assert
            wait.WaitOne(TimeSpan.FromSeconds(5));
            Assert.True(hasBeenDispatched);
        }
    }
}

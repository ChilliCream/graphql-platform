using Xunit;

namespace GreenDonut
{
    public class AutoBatchSchedulerTests
    {
        [Fact]
        public void DispatchOnEnqueue()
        {
            var dispatched = false;
            AutoBatchScheduler.Default.Schedule(() =>
            {
                dispatched = true;
                return default;
            });
            Assert.True(dispatched);
        }
    }
}

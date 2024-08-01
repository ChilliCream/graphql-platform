using Xunit;

namespace GreenDonut;

public class AutoBatchSchedulerTests
{
    [Fact]
    public void DispatchOnEnqueue()
    {
        // arrange
        var dispatched = false;
        var waitHandle = new AutoResetEvent(false);

        // act
        AutoBatchScheduler.Default.Schedule(() =>
        {
            dispatched = true;
            waitHandle.Set();
            return default;
        });

        // assert
        waitHandle.WaitOne(TimeSpan.FromSeconds(5));
        Assert.True(dispatched);
    }
}

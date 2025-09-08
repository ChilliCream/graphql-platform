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
        AutoBatchScheduler.Default.Schedule(
            new NoopBatch(() =>
            {
                dispatched = true;
                waitHandle.Set();
            }));

        // assert
        waitHandle.WaitOne(TimeSpan.FromSeconds(5));
        Assert.True(dispatched);
    }

    public class NoopBatch(Action action) : Batch
    {
        public override int Size { get; }
        public override BatchStatus Status { get; }
        public override long ModifiedTimestamp { get; }
        public override bool Touch()
        {
            throw new NotImplementedException();
        }

        public override Task DispatchAsync()
        {
            action();
            return Task.CompletedTask;
        }
    }
}

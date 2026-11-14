namespace HotChocolate.Execution;

public class ExecutionTaskTests
{
    [Fact]
    public void Reset_Should_ClearNextAndPrevious_When_TaskHasLinks()
    {
        // arrange
        var task = new TestExecutionTask();
        var next = new TestExecutionTask();
        var previous = new TestExecutionTask();
#pragma warning disable CS0618
        task.Next = next;
        task.Previous = previous;

        // act
        task.ResetTask();

        // assert
        Assert.Null(task.Next);
        Assert.Null(task.Previous);
#pragma warning restore CS0618
    }

    private sealed class TestExecutionTask : ExecutionTask
    {
        public override int BranchId => 0;

        public override bool IsDeferred => false;

        protected override IExecutionTaskContext Context => throw new NotSupportedException();

        public void ResetTask() => Reset();

        protected override ValueTask ExecuteAsync(CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }
}

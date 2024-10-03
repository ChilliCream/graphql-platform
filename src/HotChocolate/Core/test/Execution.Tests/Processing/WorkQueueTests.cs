namespace HotChocolate.Execution.Processing;

public class WorkQueueTests
{
    [Fact]
    public void Enqueue_One()
    {
        // arrange
        var queue = new WorkQueue();
        var task = new MockExecutionTask();

        // act
        queue.Push(task);

        // assert
        Assert.False(queue.HasRunningTasks);
        Assert.False(queue.IsEmpty);
    }

    [Fact]
    public void Enqueue_Two()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();

        // act
        queue.Push(task1);
        queue.Push(task2);

        // assert
        Assert.False(queue.HasRunningTasks);
        Assert.False(queue.IsEmpty);
    }

    [Fact]
    public void Take_One()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();
        queue.Push(task1);
        queue.Push(task2);

        // act
        var success = queue.TryTake(out var task);

        // assert
        Assert.Same(task2, task);
        Assert.True(success);
        Assert.True(queue.HasRunningTasks);
        Assert.False(queue.IsEmpty);
    }

    [Fact]
    public void Take_All()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();
        queue.Push(task1);
        queue.Push(task2);

        // act
        queue.TryTake(out var task);
        var success = queue.TryTake(out task);

        // assert
        Assert.Same(task1, task);
        Assert.True(success);
        Assert.True(queue.HasRunningTasks);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void Complete_All()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();
        queue.Push(task1);
        queue.Push(task2);

        // act
        queue.TryTake(out var task);
        queue.Complete();
        queue.TryTake(out task);
        queue.Complete();

        // assert
        Assert.Same(task1, task);
        Assert.False(queue.HasRunningTasks);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void Clear()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();
        queue.Push(task1);
        queue.Push(task2);

        // act
        queue.Clear();

        // assert
        Assert.False(queue.HasRunningTasks);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void New()
    {
        var queue = new WorkQueue();
        Assert.False(queue.HasRunningTasks);
        Assert.True(queue.IsEmpty);
    }

    public class MockExecutionTask : IExecutionTask
    {
        public ExecutionTaskKind Kind { get; }
        public ExecutionTaskStatus Status { get; }
        public IExecutionTask? Next { get; set; }
        public IExecutionTask? Previous { get; set; }
        public object? State { get; set; }
        public bool IsSerial { get; set; }
        public bool IsRegistered { get; set; }

        public void BeginExecute(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task WaitForCompletionAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

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
        queue.TryTake(out _);
        var success = queue.TryTake(out var task);

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
        queue.TryTake(out _);
        queue.Complete();
        queue.TryTake(out var task);
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

    [Fact]
    public void Enqueue_Deferred_Task()
    {
        // arrange
        var queue = new WorkQueue();
        var task = new MockExecutionTask(isDeferred: true);

        // act
        queue.Push(task);

        // assert
        Assert.False(queue.HasRunningTasks);
        Assert.False(queue.IsEmpty);
    }

    [Fact]
    public void Immediate_Tasks_Take_Priority_Over_Deferred()
    {
        // arrange
        var queue = new WorkQueue();
        var deferredTask = new MockExecutionTask(isDeferred: true);
        var immediateTask = new MockExecutionTask(isDeferred: false);

        // Push deferred task first
        queue.Push(deferredTask);
        // Then push immediate task
        queue.Push(immediateTask);

        // act
        queue.TryTake(out var firstTask);
        queue.TryTake(out var secondTask);

        // assert
        Assert.Same(immediateTask, firstTask);
        Assert.Same(deferredTask, secondTask);
    }

    [Fact]
    public void Mixed_Immediate_And_Deferred_Tasks()
    {
        // arrange
        var queue = new WorkQueue();
        var immediate1 = new MockExecutionTask(isDeferred: false);
        var deferred1 = new MockExecutionTask(isDeferred: true);
        var immediate2 = new MockExecutionTask(isDeferred: false);
        var deferred2 = new MockExecutionTask(isDeferred: true);

        // act
        queue.Push(immediate1);
        queue.Push(deferred1);
        queue.Push(immediate2);
        queue.Push(deferred2);

        // assert - all immediate tasks should be taken before deferred
        Assert.True(queue.TryTake(out var task1));
        Assert.Same(immediate2, task1); // LIFO for immediate stack

        Assert.True(queue.TryTake(out var task2));
        Assert.Same(immediate1, task2);

        Assert.True(queue.TryTake(out var task3));
        Assert.Same(deferred2, task3); // LIFO for deferred stack

        Assert.True(queue.TryTake(out var task4));
        Assert.Same(deferred1, task4);

        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void TryTake_Empty_Queue_Returns_False()
    {
        // arrange
        var queue = new WorkQueue();

        // act
        var success = queue.TryTake(out var task);

        // assert
        Assert.False(success);
        Assert.Null(task);
        Assert.False(queue.HasRunningTasks);
    }

    [Fact]
    public void Complete_Returns_True_When_All_Complete()
    {
        // arrange
        var queue = new WorkQueue();
        var task = new MockExecutionTask();
        queue.Push(task);
        queue.TryTake(out _);

        // act
        var allComplete = queue.Complete();

        // assert
        Assert.True(allComplete);
        Assert.False(queue.HasRunningTasks);
    }

    [Fact]
    public void Complete_Returns_False_When_More_Tasks_Running()
    {
        // arrange
        var queue = new WorkQueue();
        var task1 = new MockExecutionTask();
        var task2 = new MockExecutionTask();
        queue.Push(task1);
        queue.Push(task2);
        queue.TryTake(out _);
        queue.TryTake(out _);

        // act
        var allComplete = queue.Complete();

        // assert
        Assert.False(allComplete);
        Assert.True(queue.HasRunningTasks);
    }

    [Fact]
    public void Complete_Without_Take_Throws()
    {
        // arrange
        var queue = new WorkQueue();

        // act & assert
        Assert.Throws<InvalidOperationException>(() => queue.Complete());
    }

    [Fact]
    public void Clear_Resets_Running_Counter()
    {
        // arrange
        var queue = new WorkQueue();
        var task = new MockExecutionTask();
        queue.Push(task);
        queue.TryTake(out _);

        // act
        queue.Clear();

        // assert
        Assert.False(queue.HasRunningTasks);
        Assert.True(queue.IsEmpty);
    }

    [Fact]
    public void Push_Null_Throws_ArgumentNullException()
    {
        // arrange
        var queue = new WorkQueue();

        // act & assert
        Assert.Throws<ArgumentNullException>(() => queue.Push(null!));
    }

    [Fact]
    public void Deferred_Tasks_Only()
    {
        // arrange
        var queue = new WorkQueue();
        var deferred1 = new MockExecutionTask(isDeferred: true);
        var deferred2 = new MockExecutionTask(isDeferred: true);

        // act
        queue.Push(deferred1);
        queue.Push(deferred2);

        // assert - LIFO order
        Assert.True(queue.TryTake(out var task1));
        Assert.Same(deferred2, task1);

        Assert.True(queue.TryTake(out var task2));
        Assert.Same(deferred1, task2);

        Assert.True(queue.IsEmpty);
    }

    public class MockExecutionTask(bool isDeferred = false) : IExecutionTask
    {
        public uint Id { get; set; }
        public ExecutionTaskKind Kind { get; }
        public ExecutionTaskStatus Status { get; }
        public IExecutionTask? Next { get; set; }
        public IExecutionTask? Previous { get; set; }
        public object? State { get; set; }
        public bool IsSerial { get; set; }
        public bool IsRegistered { get; set; }

        public int BranchId => throw new NotImplementedException();

        public bool IsDeferred { get; } = isDeferred;

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

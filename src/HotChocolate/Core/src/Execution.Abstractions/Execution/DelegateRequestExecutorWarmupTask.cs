namespace HotChocolate.Execution;

public sealed class DelegateRequestExecutorWarmupTask(Func<IRequestExecutor, CancellationToken, Task> warmupFunc)
    : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => false;

    public Task WarmupAsync(IRequestExecutor requestExecutor, CancellationToken cancellationToken)
    {
        return warmupFunc.Invoke(requestExecutor, cancellationToken);
    }
}

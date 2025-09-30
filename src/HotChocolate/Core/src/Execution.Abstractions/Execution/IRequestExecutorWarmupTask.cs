namespace HotChocolate.Execution;

public interface IRequestExecutorWarmupTask
{
    Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken);
}

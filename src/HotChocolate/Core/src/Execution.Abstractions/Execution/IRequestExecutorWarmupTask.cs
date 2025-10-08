namespace HotChocolate.Execution;

/// <summary>
/// Represents a task to be run on a <see cref="IRequestExecutor"/>
/// before it's ready to handle requests.
/// </summary>
public interface IRequestExecutorWarmupTask
{
    /// <summary>
    /// Warms up the <paramref name="executor"/>.
    /// </summary>
    Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken);
}

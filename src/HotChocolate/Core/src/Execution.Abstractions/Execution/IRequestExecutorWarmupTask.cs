namespace HotChocolate.Execution;

/// <summary>
/// Represents a task to be run on a <see cref="IRequestExecutor"/>
/// before it's ready to handle requests.
/// </summary>
public interface IRequestExecutorWarmupTask
{
    /// <summary>
    /// Specifies whether the warmup task should be only applied on startup,
    /// but not subsequent request executor creations.
    /// </summary>
    bool ApplyOnlyOnStartup { get; }

    /// <summary>
    /// Warms up the <paramref name="executor"/>.
    /// </summary>
    Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken);
}

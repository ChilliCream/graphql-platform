namespace HotChocolate.Execution;

/// <summary>
/// Allows to run the initial warmup for registered <see cref="IRequestExecutor"/>s.
/// </summary>
internal interface IRequestExecutorWarmup
{
    /// <summary>
    /// Runs the initial warmup tasks.
    /// </summary>
    /// <param name="cancellationToken">
    /// The cancellation token.
    /// </param>
    /// <returns>
    /// Returns a task that completes once the warmup is done.
    /// </returns>
    Task WarmupAsync(CancellationToken cancellationToken);
}

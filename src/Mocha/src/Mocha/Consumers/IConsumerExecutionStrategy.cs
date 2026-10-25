namespace Mocha;

/// <summary>
/// Executes awaited, sequential consumer attempts, optionally repeating them according to an infrastructure policy.
/// Implementations must not retain the callback after execution completes.
/// </summary>
public interface IConsumerExecutionStrategy
{
    /// <summary>
    /// Executes the supplied consumer attempt.
    /// </summary>
    /// <param name="context">The consume context the attempts are created from.</param>
    /// <param name="executeAttempt">The callback that creates and executes one consumer attempt.</param>
    ValueTask ExecuteAsync(IConsumeContext context, Func<CancellationToken, ValueTask> executeAttempt);
}

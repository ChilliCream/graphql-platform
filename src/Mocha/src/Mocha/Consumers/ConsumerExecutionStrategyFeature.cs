namespace Mocha;

/// <summary>
/// A message bus feature that selects the <see cref="IConsumerExecutionStrategy"/> used to run consumer attempts.
/// </summary>
public sealed class ConsumerExecutionStrategyFeature
{
    /// <summary>
    /// Gets or sets the strategy that executes consumer attempts.
    /// </summary>
    public required IConsumerExecutionStrategy Strategy { get; set; }
}

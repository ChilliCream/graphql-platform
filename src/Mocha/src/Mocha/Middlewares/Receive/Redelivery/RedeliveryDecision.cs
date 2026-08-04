namespace Mocha;

/// <summary>
/// The result of evaluating exception policy rules for a redelivery decision.
/// </summary>
internal readonly struct RedeliveryDecision
{
    private RedeliveryDecision(RedeliveryAction action, TimeSpan delay = default)
    {
        Action = action;
        Delay = delay;
    }

    /// <summary>
    /// Gets the action to take.
    /// </summary>
    public RedeliveryAction Action { get; }

    /// <summary>
    /// Gets the delay before redelivery. Only meaningful when <see cref="Action"/> is
    /// <see cref="RedeliveryAction.Redeliver"/>.
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    /// A decision to re-throw the exception.
    /// </summary>
    public static readonly RedeliveryDecision Rethrow = new(RedeliveryAction.Rethrow);

    /// <summary>
    /// A decision to discard the message.
    /// </summary>
    public static readonly RedeliveryDecision Discard = new(RedeliveryAction.Discard);

    /// <summary>
    /// Creates a decision to redeliver the message after the specified delay.
    /// </summary>
    /// <param name="delay">The delay before redelivery.</param>
    /// <returns>A redeliver decision.</returns>
    public static RedeliveryDecision Redeliver(TimeSpan delay) => new(RedeliveryAction.Redeliver, delay);
}

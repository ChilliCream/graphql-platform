namespace Mocha;

/// <summary>
/// Represents a single per-exception policy rule with optional retry, redelivery, and terminal actions.
/// </summary>
public sealed class ExceptionPolicyRule
{
    /// <summary>
    /// Gets the exception type this rule applies to.
    /// </summary>
    public required Type ExceptionType { get; init; }

    /// <summary>
    /// Gets the optional predicate to further filter the exception.
    /// </summary>
    public required Func<Exception, bool>? Predicate { get; init; }

    /// <summary>
    /// Gets or sets the retry configuration for this exception.
    /// </summary>
    public RetryPolicyConfig? Retry { get; set; }

    /// <summary>
    /// Gets or sets the redelivery configuration for this exception.
    /// </summary>
    public RedeliveryPolicyConfig? Redelivery { get; set; }

    /// <summary>
    /// Gets or sets the terminal action for this exception.
    /// </summary>
    public TerminalAction? Terminal { get; set; }
}

/// <summary>
/// Specifies the terminal action to take for an exception.
/// </summary>
public enum TerminalAction
{
    /// <summary>
    /// Routes the message to the error endpoint.
    /// </summary>
    DeadLetter,

    /// <summary>
    /// Swallows the exception; the message disappears.
    /// </summary>
    Discard
}

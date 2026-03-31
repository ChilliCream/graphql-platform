namespace Mocha;

/// <summary>
/// Internal representation of an exception filtering rule.
/// </summary>
internal sealed class ExceptionRule
{
    /// <summary>
    /// Gets the exception type this rule applies to.
    /// </summary>
    public required Type ExceptionType { get; init; }

    /// <summary>
    /// Gets the optional predicate that further filters the exception.
    /// </summary>
    public required Func<Exception, bool>? Predicate { get; init; }

    /// <summary>
    /// Gets the action to take when this rule matches.
    /// </summary>
    public required ExceptionAction Action { get; init; }
}

/// <summary>
/// Actions that can be taken for a matched exception rule.
/// </summary>
internal enum ExceptionAction
{
    /// <summary>
    /// Don't retry/redeliver this exception.
    /// </summary>
    Ignore
}

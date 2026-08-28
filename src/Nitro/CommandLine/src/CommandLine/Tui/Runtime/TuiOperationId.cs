namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// Identifies one asynchronous effect submitted to a <see cref="TuiEffectQueue{TResult}"/>.
/// Assigned once at submission and carried through to its completion, so a caller can
/// discover an operation's outcome, or its absence, after the event loop has moved on.
/// </summary>
internal readonly record struct TuiOperationId(Guid Value)
{
    /// <summary>
    /// Assigns a new, globally unique operation ID.
    /// </summary>
    public static TuiOperationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

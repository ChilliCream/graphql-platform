namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// Identifies an asynchronous effect from submission through completion.
/// </summary>
internal readonly record struct TuiOperationId(Guid Value)
{
    /// <summary>
    /// Assigns a new, globally unique operation ID.
    /// </summary>
    public static TuiOperationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("N");
}

namespace ChilliCream.Nitro.CommandLine.Commands.Schemas.Components;

/// <summary>
/// The verdict emitted by <c>nitro schema impact</c>. Answers the "what happens if I remove
/// this coordinate" question in a single string a coding agent can switch on.
/// </summary>
internal enum CoordinateRemovalVerdict
{
    /// <summary>
    /// Zero usage in the requested window and the coordinate is not deprecated — safe to
    /// drop directly without a deprecation cycle.
    /// </summary>
    SafeToRemove,

    /// <summary>
    /// Deprecated and zero usage in the requested window — the deprecation window has
    /// elapsed and the coordinate is safe to drop.
    /// </summary>
    ReadyToRemove,

    /// <summary>
    /// Deprecated but still used — notify affected clients and tighten the deprecation
    /// window.
    /// </summary>
    DeprecatedInUse,

    /// <summary>
    /// Not deprecated and still used — deprecate first, wait, then re-measure.
    /// </summary>
    UnsafeToRemove
}

/// <summary>
/// The analytical payload rendered by <c>nitro schema impact</c>. Combines the usage
/// summary with the client and operation breakdown used by <c>clients</c> and
/// <c>operations</c>, plus a computed <c>Verdict</c> string. The verdict is serialised
/// as the upper snake case name of a <see cref="CoordinateRemovalVerdict"/> so JSON
/// consumers can switch on it without caring about the C# enum identity.
/// </summary>
internal sealed record CoordinateImpactResult
{
    public required string Coordinate { get; init; }

    public required bool IsDeprecated { get; init; }

    public required string Verdict { get; init; }

    public required CoordinateUsageResult Usage { get; init; }

    public required IReadOnlyList<CoordinateClientsResultEntry> Clients { get; init; }

    public required IReadOnlyList<CoordinateOperationsResultEntry> Operations { get; init; }
}

/// <summary>
/// The client-side verdict calculator for <c>nitro schema impact</c>. Mirrors the rule
/// table defined in the plan.
/// </summary>
internal static class VerdictCalculator
{
    public static CoordinateRemovalVerdict Compute(bool isDeprecated, long totalRequests)
    {
        if (totalRequests == 0L)
        {
            return isDeprecated
                ? CoordinateRemovalVerdict.ReadyToRemove
                : CoordinateRemovalVerdict.SafeToRemove;
        }

        return isDeprecated
            ? CoordinateRemovalVerdict.DeprecatedInUse
            : CoordinateRemovalVerdict.UnsafeToRemove;
    }

    public static string ToSerializedString(CoordinateRemovalVerdict verdict)
    {
        return verdict switch
        {
            CoordinateRemovalVerdict.SafeToRemove => "SAFE_TO_REMOVE",
            CoordinateRemovalVerdict.ReadyToRemove => "READY_TO_REMOVE",
            CoordinateRemovalVerdict.DeprecatedInUse => "DEPRECATED_IN_USE",
            CoordinateRemovalVerdict.UnsafeToRemove => "UNSAFE_TO_REMOVE",
            _ => throw new ArgumentOutOfRangeException(nameof(verdict), verdict, null)
        };
    }
}

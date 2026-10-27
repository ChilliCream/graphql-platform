namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// The result of running a <see cref="TuiQuitGate"/>: how many operations were still
/// pending after its bounded drain, how many resolved to an outcome the feature itself
/// could not classify, and which operation IDs remain discoverable afterward.
/// </summary>
internal readonly record struct TuiQuitGateReport(
    int PendingCount,
    int OutcomeUnknownCount,
    IReadOnlyList<TuiOperationId> DiscoverableOperationIds)
{
    /// <summary>
    /// No unresolved work: a confirmed normal quit may proceed without asking again.
    /// </summary>
    public static readonly TuiQuitGateReport Clear = new(0, 0, []);

    /// <summary>
    /// Whether either the pending count or the unknown-outcome count is positive.
    /// </summary>
    public bool HasUnresolvedWork => PendingCount > 0 || OutcomeUnknownCount > 0;
}

/// <summary>
/// Stops new feature submissions, waits within the supplied bound, and reports
/// unresolved work before a confirmed quit. The host bypasses gates on cancellation
/// and must resume submissions if the user cancels the quit confirmation.
/// </summary>
/// <param name="drainBound">The bounded wait for in-flight effects to resolve.</param>
/// <param name="cancellationToken">Cancels the wait early; the drain itself is best-effort.</param>
internal delegate Task<TuiQuitGateReport> TuiQuitGate(TimeSpan drainBound, CancellationToken cancellationToken);

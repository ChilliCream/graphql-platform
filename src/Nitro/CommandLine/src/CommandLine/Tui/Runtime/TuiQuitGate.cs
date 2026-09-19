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
    /// Whether this report has anything a normal quit should surface to the user
    /// before it is allowed to cancel the event loop.
    /// </summary>
    public bool HasUnresolvedWork => PendingCount > 0 || OutcomeUnknownCount > 0;
}

/// <summary>
/// Stops a feature's own effect submissions and bounded-drains whatever is already in
/// flight, reporting what remained unresolved afterward. Every registered gate runs
/// before a normal confirmed quit is allowed to fire. Ctrl+C and host cancellation
/// bypass this gate entirely. A feature that registers a gate must subscribe to
/// <c>TuiShell.QuitCancelled</c> and call its queue's <c>ResumeAccepting</c>.
/// </summary>
/// <param name="drainBound">The bounded wait for in-flight effects to resolve.</param>
/// <param name="cancellationToken">Cancels the wait early; the drain itself is best-effort.</param>
internal delegate Task<TuiQuitGateReport> TuiQuitGate(TimeSpan drainBound, CancellationToken cancellationToken);

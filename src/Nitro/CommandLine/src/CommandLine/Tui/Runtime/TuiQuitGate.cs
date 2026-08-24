namespace ChilliCream.Nitro.CommandLine.Tui.Runtime;

/// <summary>
/// The result of running a <see cref="TuiQuitGate"/>: how many operations were still
/// pending after its bounded drain, how many resolved to an outcome the feature itself
/// could not classify (a judgment the generic runtime never makes on its own), and
/// which operation IDs remain discoverable for a later view such as a dashboard or
/// doctor command.
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
/// flight, reporting what remained unresolved afterward. A normal confirmed quit runs
/// every registered gate before it is allowed to fire the shell's own quit-confirmed
/// notification, so the event loop is never cancelled out from under an effect that
/// could still resolve into a lost result. Ctrl+C and host cancellation bypass this
/// gate entirely and stay noninteractive: whatever remains in flight stays
/// discoverable through the owning <see cref="TuiEffectQueue{TResult}"/>'s own
/// <c>PendingOperationIds</c> instead. A feature that registers a gate must subscribe
/// to <c>TuiShell.QuitCancelled</c> and call its queue's <c>ResumeAccepting</c>, since a
/// cancelled second confirmation returns the user to a live TUI.
/// </summary>
/// <param name="drainBound">The bounded wait for in-flight effects to resolve.</param>
/// <param name="cancellationToken">Cancels the wait early; the drain itself is best-effort.</param>
internal delegate Task<TuiQuitGateReport> TuiQuitGate(TimeSpan drainBound, CancellationToken cancellationToken);

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions</c> lifecycle: bind, claim, reap, and list, all
/// predicated on the full generation identity in
/// <see cref="AgentSessionGeneration"/> so a stale caller can never mutate a
/// row a newer generation now owns. Backend-agnostic: no member exposes
/// ADO.NET or SQLite types.
/// </summary>
internal interface IAgentSessionRegistry
{
    /// <summary>
    /// Upserts the row for <paramref name="generation"/>'s
    /// <c>(harness, session_id)</c>, the SessionStart binding rules:
    /// <list type="bullet">
    /// <item>No existing row: inserts one, bound
    /// (<c>binding_kind = 'env'</c>) when <paramref name="envActor"/> is
    /// given, otherwise unclaimed.</item>
    /// <item>An existing row at the SAME generation: a duplicate delivery,
    /// preserves binding, ledger, and counters, only refreshing the
    /// heartbeat.</item>
    /// <item>An existing row at a DIFFERENT generation: a new process
    /// replaced the one the row remembered, rebinds per
    /// <paramref name="envActor"/> exactly like a missing row would, and
    /// resets the delivery ledger and counters.</item>
    /// </list>
    /// </summary>
    Task<AgentSessionRecord> StartAsync(
        AgentSessionGeneration generation,
        string cwd,
        string workspacePath,
        string endpointKind,
        string endpointAddr,
        string? envActor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Applies the claim state machine to the row matching
    /// <paramref name="generation"/> exactly (harness, session id, host,
    /// pid, and proc_start all predicate the row lookup, so a stale
    /// generation matches nothing). Throws <see cref="ExitException"/> when
    /// no row matches that generation, or when the row is already
    /// explicitly claimed by a different actor and
    /// <paramref name="forceRebind"/> is false.
    /// </summary>
    Task<AgentSessionClaimResult> ClaimAsync(
        AgentSessionGeneration generation,
        string actor,
        bool forceRebind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Zero-config self-claim: resolves this process's Claude Code ancestor
    /// session (Linux-first, see <see cref="IClaudeAncestorSessionResolver"/>),
    /// bootstraps its row via <see cref="StartAsync"/> when none exists yet,
    /// then applies <see cref="ClaimAsync"/> for <paramref name="actor"/>.
    /// Throws <see cref="ExitException"/> when no Claude Code ancestor can be
    /// found, its process is no longer running, or no agent workspace
    /// resolves from its working directory.
    /// </summary>
    Task<AgentSessionClaimResult> SelfClaimAsync(
        string actor,
        bool forceRebind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conditionally deletes the row matching <paramref name="generation"/>
    /// exactly. A late call against a generation the row no longer carries
    /// (superseded by a fresh SessionStart, already reaped) is a no-op.
    /// Returns whether a row was actually deleted.
    /// </summary>
    Task<bool> EndAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every row on the CURRENT Nitro instance whose process is no
    /// longer alive at its recorded generation. Rows recorded by a different
    /// instance id are never touched. Returns the rows that were reaped.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reaps dead current-instance rows, then returns every surviving row
    /// with its computed <see cref="AgentSessionState"/>.
    /// </summary>
    Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken);
}

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>agent_sessions</c> lifecycle: bind, claim, heartbeat, reap, and
/// list, all predicated on the full generation identity in
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
    /// Returns the row matching <paramref name="generation"/> exactly (the
    /// full generation predicate, not just harness and session id), or null
    /// when no row matches. Used by the hook adapters to require a claimed
    /// row belonging to the exact process instance a turn-boundary event
    /// fired against before acting on it.
    /// </summary>
    Task<AgentSessionRecord?> FindByGenerationAsync(
        AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Resets <c>block_budget_used</c> to zero for the row matching
    /// <paramref name="generation"/> exactly. A generation that matches no
    /// row is a no-op. Called on <c>UserPromptSubmit</c>, so a lifetime
    /// ceiling can never silently disable the Stop gate.
    /// </summary>
    Task ResetBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments <c>block_budget_used</c> by one for the row
    /// matching <paramref name="generation"/> exactly and returns the new
    /// value. Returns null when no row matches that generation.
    /// </summary>
    Task<int?> IncrementBlockBudgetAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes every row on the CURRENT Nitro instance this reader can PROVE
    /// dead in the same observable process scope as its recorded generation
    /// (see <see cref="IProcessInfoProvider.Observe"/>). A row this reader
    /// cannot observe, typically a different PID namespace than the row's
    /// writer recorded, is left untouched, the same as a row still alive.
    /// Rows recorded by a different instance id are never touched. Returns
    /// the rows that were reaped.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> ReapAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Reaps provably dead current-instance rows, then returns every
    /// surviving row with its computed <see cref="AgentSessionState"/>,
    /// including <see cref="AgentSessionState.Unobservable"/> for a
    /// current-instance row this reader cannot verify.
    /// </summary>
    Task<IReadOnlyList<AgentSessionView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Advances <c>last_beat_at</c> to now for the row matching <paramref
    /// name="generation"/> exactly, without changing binding, role,
    /// counters, endpoints, or delivery ledgers. A generation that matches
    /// no row (already ended or superseded) is a no-op. Returns whether a
    /// row was actually touched.
    /// </summary>
    Task<bool> TouchAsync(AgentSessionGeneration generation, CancellationToken cancellationToken);

    /// <summary>
    /// Records <paramref name="harnessVersion"/> for the row matching
    /// <paramref name="generation"/> exactly, without changing any other
    /// column. A generation that matches no row (already ended or
    /// superseded) is a no-op. Returns whether a row was actually updated.
    /// </summary>
    Task<bool> RecordHarnessVersionAsync(
        AgentSessionGeneration generation, string harnessVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically upserts the durable identity for <paramref name="actor"/>
    /// and binds/promotes it onto the row matching <paramref
    /// name="generation"/> exactly, applying the same claim state machine as
    /// <see cref="ClaimAsync"/> and persisting <paramref name="role"/>
    /// (normalized) onto the participant. Both writes commit or roll back
    /// together. Throws <see cref="ExitException"/> when no row matches that
    /// generation, when this process cannot verify it against the row's
    /// recorded process scope, or when the row is already explicitly
    /// claimed by a different actor and <paramref name="forceRebind"/> is
    /// false. Repeating the same actor and role is idempotent: it still
    /// refreshes the identity's last-seen time and the participant's
    /// heartbeat, but reports no change.
    /// </summary>
    Task<AgentSessionRegisterResult> RegisterAsync(
        AgentSessionGeneration generation,
        string actor,
        string role,
        string client,
        bool forceRebind,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every row matching <paramref name="harness"/>, <paramref
    /// name="host"/>, <paramref name="pid"/>, and <paramref
    /// name="procStart"/> exactly, for resolving a session by its process
    /// identity when its session id is not independently known. Empty when
    /// none matches; more than one means the match is ambiguous.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> FindByProcessAsync(
        string harness, string host, int pid, DateTimeOffset procStart, CancellationToken cancellationToken);

    /// <summary>
    /// Reaps dead current-instance rows, then returns every surviving row
    /// bound to <paramref name="agentName"/> on the CURRENT instance (remote
    /// rows are never returned: a ping fired from here cannot reach a
    /// session another Nitro instance owns). Used by the notifier and
    /// <c>nitro agent ping</c> to resolve which sessions to fire at.
    /// </summary>
    Task<IReadOnlyList<AgentSessionRecord>> FindLiveClaimedByAgentNameAsync(
        string agentName, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims the per-session ping cooldown for
    /// <paramref name="session"/>'s exact generation: succeeds, stamping
    /// <paramref name="attemptId"/> as the new pending attempt and clearing
    /// any previous result, only when no prior attempt was claimed within
    /// <paramref name="cooldown"/> of <paramref name="now"/>. Returns false
    /// (a no-op) when the cooldown is still active or the generation no
    /// longer matches a row - both cases mean the caller must not act.
    /// </summary>
    Task<bool> TryClaimPingCooldownAsync(
        AgentSessionRecord session,
        string attemptId,
        DateTimeOffset now,
        TimeSpan cooldown,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes a ping outcome for <paramref name="sessionId"/>, conditioned
    /// on <paramref name="attemptId"/> still being the row's current
    /// <c>last_ping_attempt</c>: an out-of-order or superseded completion
    /// (the row rebound to a new generation, or a newer attempt already
    /// claimed the cooldown) affects zero rows instead of overwriting a
    /// newer result.
    /// </summary>
    Task WritePingResultAsync(
        string harness,
        string sessionId,
        string attemptId,
        string result,
        string? detail,
        CancellationToken cancellationToken);
}

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Reads and writes the unified <c>agents</c> table: identity, harness session binding,
/// role, and presence.
/// </summary>
internal interface IAgentStore
{
    /// <summary>
    /// Mints a login-only agent with an empty role and no harness or session.
    /// </summary>
    Task<AgentRow> LoginAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts or resumes a harness session. Reuses the row for a known, non-deleted
    /// harness session id, mints a new agent for an unknown one, or returns
    /// <see cref="AgentSessionStartKind.Ignored"/> without writing anything when the
    /// harness session id belongs to a deleted agent.
    /// </summary>
    Task<AgentSessionStartResult> StartSessionAsync(
        AgentSessionStartRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes last-seen time for the agent bound to the given harness session id.
    /// Returns false, writing nothing, when no matching non-deleted row exists.
    /// </summary>
    Task<bool> TouchSessionAsync(string harness, string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name and refreshes its last-seen time. Returns false,
    /// writing nothing, when no matching non-deleted row exists.
    /// </summary>
    Task<bool> TouchAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the agent bound to the given harness session id as ended. Returns false,
    /// writing nothing, when no matching non-deleted row exists.
    /// </summary>
    Task<bool> EndSessionAsync(string harness, string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name and role, writes the role, and refreshes last-seen
    /// time. Returns null, writing nothing, when no matching non-deleted row exists.
    /// </summary>
    Task<AgentRow?> SetRoleAsync(string name, string role, CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name and returns the matching row, including a deleted one,
    /// or null when it does not exist.
    /// </summary>
    Task<AgentRow?> FindAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the row bound to the given harness session id, including a deleted one,
    /// or null when it does not exist.
    /// </summary>
    Task<AgentRow?> FindBySessionAsync(string harness, string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Returns every non-deleted agent row, in no particular order.
    /// </summary>
    Task<IReadOnlyList<AgentRow>> ListAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes and writes the agent's transport endpoint, retaining the credential only
    /// for an opencode harness's opencode-server endpoint. Returns false, writing nothing,
    /// when no matching non-deleted row exists.
    /// </summary>
    Task<bool> SetEndpointAsync(
        string name,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        CancellationToken cancellationToken);

    /// <summary>
    /// Resets the agent's block budget to zero. Returns 0, writing nothing, when no matching
    /// non-deleted row exists.
    /// </summary>
    Task<int> ResetBlockBudgetAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically increments the agent's block budget by one and returns the new value.
    /// Returns 0, writing nothing, when no matching non-deleted row exists.
    /// </summary>
    Task<int> IncrementBlockBudgetAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Claims the agent's ping cooldown for <paramref name="attemptId"/>, clearing the
    /// previous result and detail. Returns false, writing nothing, when no matching
    /// non-deleted row exists or the previous attempt is newer than now minus
    /// <paramref name="cooldown"/>.
    /// </summary>
    Task<bool> TryClaimPingCooldownAsync(
        string name, TimeSpan cooldown, string attemptId, CancellationToken cancellationToken);

    /// <summary>
    /// Records the ping result and detail only when the agent name and its current ping
    /// attempt id match. A stale attempt id, a missing agent, or a deleted agent changes
    /// nothing.
    /// </summary>
    Task WritePingResultAsync(
        string name, string attemptId, string result, string? detail, CancellationToken cancellationToken);

    /// <summary>
    /// Marks the agent's actor announcement as pending. A missing or deleted agent is unchanged.
    /// </summary>
    Task ArmAnnouncementAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically clears the agent's pending announcement and returns whether one was pending.
    /// Returns false when no matching non-deleted row exists or none is pending.
    /// </summary>
    Task<bool> ClaimAnnouncementAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Returns whether the agent has a pending announcement. Returns false when no matching
    /// non-deleted row exists.
    /// </summary>
    Task<bool> IsAnnouncementPendingAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Arms one idle push for the agent. A missing or deleted agent is unchanged.
    /// </summary>
    Task RearmIdlePushAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically consumes the agent's armed idle push and returns whether one was armed.
    /// Returns false when no matching non-deleted row exists or none is armed.
    /// </summary>
    Task<bool> ClaimIdlePushAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Updates the agent's harness version. Returns false, writing nothing, when no matching
    /// non-deleted row exists.
    /// </summary>
    Task<bool> RecordHarnessVersionAsync(string name, string harnessVersion, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes the named agent: stamps <c>deleted_at</c>, clears its endpoint and
    /// transient state, drops its wake and ping side rows, and returns its in-progress
    /// tasks to open and unassigned. Returns false, writing nothing, when no matching
    /// non-deleted row exists. There is no undelete.
    /// </summary>
    Task<bool> DeleteAsync(string name, CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes every agent currently resolving to <see cref="AgentState.Offline"/> or
    /// <see cref="AgentState.Idle"/>, applying the same effect as <see cref="DeleteAsync"/> to
    /// each. Online and unreachable agents are left untouched. Returns the number of agents
    /// deleted.
    /// </summary>
    Task<int> DeleteInactiveAsync(CancellationToken cancellationToken);
}

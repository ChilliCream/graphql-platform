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
}

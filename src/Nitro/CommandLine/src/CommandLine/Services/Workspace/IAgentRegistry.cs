namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Backend-agnostic registry of agents shared by every workspace feature.
/// No member exposes ADO.NET or SQLite types.
/// </summary>
internal interface IAgentRegistry
{
    /// <summary>
    /// Normalizes the given name, role, and client, then upserts the agent:
    /// sets last_seen_at to now, sets implicit to false, and sets role and
    /// client to the given values (the empty string clears either).
    /// Explicit user intent, used by the <c>register</c> command.
    /// </summary>
    Task<AgentRecord> RegisterAsync(
        string name,
        string role,
        string client,
        CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name, then upserts the agent: sets last_seen_at
    /// to now and implicit to false, without touching role. Used by the
    /// implicit auto-registration mail send and reply perform on the acting
    /// agent; a first-time insert gets an empty role.
    /// </summary>
    Task<AgentRecord> TouchAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name and returns the agent with that name, or
    /// null.
    /// </summary>
    Task<AgentRecord?> GetAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Normalizes the given name and returns the existing agent with that
    /// name unchanged, or inserts one with an empty role, last_seen_at and
    /// registered_at set to now, and implicit set to true when none exists.
    /// Used to satisfy the agents table's foreign key when a message is sent
    /// to a recipient that has never registered or acted.
    /// </summary>
    Task<AgentRecord> EnsureImplicitAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns every registered agent, ordered by name. When
    /// <paramref name="role"/> is given, only agents with that exact
    /// normalized role are returned. When <paramref name="staleBefore"/> is
    /// given, only agents whose last_seen_at is older than it are returned.
    /// </summary>
    Task<IReadOnlyList<AgentRecord>> ListAsync(
        string? role,
        DateTimeOffset? staleBefore,
        CancellationToken cancellationToken);
}

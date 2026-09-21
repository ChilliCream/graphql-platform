namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Stores and queries agent identities shared by workspace features.
/// </summary>
internal interface IAgentRegistry
{
    /// <summary>
    /// Registers the normalized name, role, and client, refreshes last-seen time, and
    /// clears the implicit flag. Empty role or client values clear those fields.
    /// </summary>
    Task<AgentRecord> RegisterAsync(
        string name,
        string role,
        string client,
        CancellationToken cancellationToken);

    /// <summary>
    /// Allocates and records an unused actor name without binding it to a session.
    /// </summary>
    Task<AgentRecord> AllocateAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Marks the normalized actor as active, refreshing last-seen time and clearing the
    /// implicit flag without changing existing role or client values. A new actor has
    /// an empty role and client.
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
    /// Returns the normalized actor unchanged if it exists, or creates an implicit
    /// identity with an empty role and client and both timestamps set to now.
    /// </summary>
    Task<AgentRecord> EnsureImplicitAsync(
        string name,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns all agent identities, including implicit ones, ordered by name. Non-null
    /// filters restrict results to the normalized role and a last-seen time strictly
    /// before <paramref name="staleBefore"/>.
    /// </summary>
    Task<IReadOnlyList<AgentRecord>> ListAsync(
        string? role,
        DateTimeOffset? staleBefore,
        CancellationToken cancellationToken);
}

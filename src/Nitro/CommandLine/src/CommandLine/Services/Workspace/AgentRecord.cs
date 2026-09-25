namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// An agent identity shared by workspace features.
/// </summary>
internal sealed record AgentRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agents table.
    /// </summary>
    public const string Columns =
        "name AS Name, role AS Role, client AS Client, implicit AS Implicit, "
        + "registered_at AS RegisteredAt, last_seen_at AS LastSeenAt";

    public required string Name { get; init; }
    public required string Role { get; init; }

    /// <summary>
    /// The normalized lowercase client program name, or empty when unknown.
    /// </summary>
    public required string Client { get; init; }

    /// <summary>
    /// True for a placeholder identity that has not been registered or marked active.
    /// </summary>
    public required bool Implicit { get; init; }

    public required DateTimeOffset RegisteredAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
}

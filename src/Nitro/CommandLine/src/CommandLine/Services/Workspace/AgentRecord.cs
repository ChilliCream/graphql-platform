namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A registered agent, shared by every workspace feature that needs
/// identity: tasks, mail, and anything added afterward.
/// </summary>
internal sealed record AgentRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agents table.
    /// </summary>
    public const string Columns =
        "name AS Name, role AS Role, implicit AS Implicit, "
        + "registered_at AS RegisteredAt, last_seen_at AS LastSeenAt";

    public required string Name { get; init; }
    public required string Role { get; init; }

    /// <summary>
    /// True when this row was created for an unknown recipient that has
    /// never acted itself, rather than by an explicit registration.
    /// </summary>
    public required bool Implicit { get; init; }

    public required DateTimeOffset RegisteredAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
}

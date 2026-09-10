namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed record AgentSessionIdentityRecord
{
    public const string Columns =
        "harness AS Harness, session_id AS SessionId, actor AS Actor, role AS Role, "
        + "actor_revision AS ActorRevision, created_at AS CreatedAt, last_seen_at AS LastSeenAt";

    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string Actor { get; init; }
    public required string Role { get; init; }
    public required long ActorRevision { get; init; }
    public string CreatedAt { get; init; } = "";
    public string LastSeenAt { get; init; } = "";
}

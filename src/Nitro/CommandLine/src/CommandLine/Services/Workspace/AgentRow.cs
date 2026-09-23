using System.Globalization;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// One row of the unified <c>agents</c> table: an agent's identity, its
/// harness session binding, presence, and wake state.
/// </summary>
internal sealed record AgentRow
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agents table.
    /// </summary>
    public const string Columns =
        "name AS Name, role AS Role, harness AS Harness, harness_version AS HarnessVersion, "
        + "session_id AS SessionId, cwd AS Cwd, workspace_path AS WorkspacePath, "
        + "registered_at AS RegisteredAt, started_at AS StartedAt, last_seen_at AS LastSeenAt, "
        + "ended_at AS EndedAt, deleted_at AS DeletedAt, endpoint_kind AS EndpointKind, "
        + "endpoint_addr AS EndpointAddr, endpoint_secret AS EndpointSecret, "
        + "block_budget_used AS BlockBudgetUsed, last_ping_at AS LastPingAt, "
        + "last_ping_attempt AS LastPingAttempt, last_ping_result AS LastPingResult, "
        + "last_ping_detail AS LastPingDetail, announcement_pending AS AnnouncementPending, "
        + "idle_push_armed AS IdlePushArmed";

    public required string Name { get; init; }
    public required string Role { get; init; }

    /// <summary>
    /// The canonical harness id, or null for a login-only agent.
    /// </summary>
    public string? Harness { get; init; }

    public required string HarnessVersion { get; init; }

    /// <summary>
    /// The harness session id, or null for a login-only agent.
    /// </summary>
    public string? SessionId { get; init; }

    public required string Cwd { get; init; }
    public required string WorkspacePath { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }

    /// <summary>
    /// When the agent's session ended, or null while it is still running.
    /// </summary>
    public DateTimeOffset? EndedAt { get; init; }

    /// <summary>
    /// When the agent was soft-deleted, or null while it is active.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; init; }

    public required string EndpointKind { get; init; }

    /// <summary>
    /// Empty only when <see cref="EndpointKind"/> is <c>none</c>.
    /// </summary>
    public required string EndpointAddr { get; init; }

    /// <summary>
    /// The credential for endpoints that require one, or null when the
    /// endpoint has no credential.
    /// </summary>
    public string? EndpointSecret { get; init; }

    public required int BlockBudgetUsed { get; init; }
    public DateTimeOffset? LastPingAt { get; init; }

    /// <summary>
    /// The most recent ping attempt id, or null before an attempt is claimed.
    /// </summary>
    public string? LastPingAttempt { get; init; }

    /// <summary>
    /// The most recent completed ping result from <see cref="AgentPingResult"/>.
    /// </summary>
    public string? LastPingResult { get; init; }

    /// <summary>
    /// The optional ping diagnostic, limited to 200 characters.
    /// </summary>
    public string? LastPingDetail { get; init; }

    public required bool AnnouncementPending { get; init; }
    public required bool IdlePushArmed { get; init; }

    /// <summary>
    /// True when the agent has been soft-deleted.
    /// </summary>
    public bool IsDeleted => DeletedAt is not null;

    /// <summary>
    /// Reads the current row using the column names declared by <see cref="Columns"/>.
    /// </summary>
    public static AgentRow ReadFrom(SqliteDataReader reader) => new()
    {
        Name = reader.GetString(reader.GetOrdinal("Name")),
        Role = reader.GetString(reader.GetOrdinal("Role")),
        Harness = reader.IsDBNull(reader.GetOrdinal("Harness"))
            ? null
            : reader.GetString(reader.GetOrdinal("Harness")),
        HarnessVersion = reader.GetString(reader.GetOrdinal("HarnessVersion")),
        SessionId = reader.IsDBNull(reader.GetOrdinal("SessionId"))
            ? null
            : reader.GetString(reader.GetOrdinal("SessionId")),
        Cwd = reader.GetString(reader.GetOrdinal("Cwd")),
        WorkspacePath = reader.GetString(reader.GetOrdinal("WorkspacePath")),
        RegisteredAt = DateTimeOffset.Parse(
            reader.GetString(reader.GetOrdinal("RegisteredAt")), CultureInfo.InvariantCulture),
        StartedAt = DateTimeOffset.Parse(
            reader.GetString(reader.GetOrdinal("StartedAt")), CultureInfo.InvariantCulture),
        LastSeenAt = DateTimeOffset.Parse(
            reader.GetString(reader.GetOrdinal("LastSeenAt")), CultureInfo.InvariantCulture),
        EndedAt = reader.IsDBNull(reader.GetOrdinal("EndedAt"))
            ? null
            : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("EndedAt")), CultureInfo.InvariantCulture),
        DeletedAt = reader.IsDBNull(reader.GetOrdinal("DeletedAt"))
            ? null
            : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("DeletedAt")), CultureInfo.InvariantCulture),
        EndpointKind = reader.GetString(reader.GetOrdinal("EndpointKind")),
        EndpointAddr = reader.GetString(reader.GetOrdinal("EndpointAddr")),
        EndpointSecret = reader.IsDBNull(reader.GetOrdinal("EndpointSecret"))
            ? null
            : reader.GetString(reader.GetOrdinal("EndpointSecret")),
        BlockBudgetUsed = reader.GetInt32(reader.GetOrdinal("BlockBudgetUsed")),
        LastPingAt = reader.IsDBNull(reader.GetOrdinal("LastPingAt"))
            ? null
            : DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("LastPingAt")), CultureInfo.InvariantCulture),
        LastPingAttempt = reader.IsDBNull(reader.GetOrdinal("LastPingAttempt"))
            ? null
            : reader.GetString(reader.GetOrdinal("LastPingAttempt")),
        LastPingResult = reader.IsDBNull(reader.GetOrdinal("LastPingResult"))
            ? null
            : reader.GetString(reader.GetOrdinal("LastPingResult")),
        LastPingDetail = reader.IsDBNull(reader.GetOrdinal("LastPingDetail"))
            ? null
            : reader.GetString(reader.GetOrdinal("LastPingDetail")),
        AnnouncementPending = reader.GetInt32(reader.GetOrdinal("AnnouncementPending")) != 0,
        IdlePushArmed = reader.GetInt32(reader.GetOrdinal("IdlePushArmed")) != 0
    };
}

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A session presence record with its actor binding, endpoint, heartbeat, and ping state.
/// </summary>
internal sealed record AgentSessionRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agent_sessions table.
    /// </summary>
    public const string Columns =
        "harness AS Harness, session_id AS SessionId, agent_name AS AgentName, "
        + "binding_kind AS BindingKind, host AS Host, cwd AS Cwd, workspace_path AS WorkspacePath, endpoint_kind AS EndpointKind, "
        + "endpoint_addr AS EndpointAddr, endpoint_secret AS EndpointSecret, started_at AS StartedAt, last_beat_at AS LastBeatAt, "
        + "block_budget_used AS BlockBudgetUsed, last_ping_at AS LastPingAt, "
        + "last_ping_attempt AS LastPingAttempt, last_ping_result AS LastPingResult, "
        + "last_ping_detail AS LastPingDetail, role AS Role, harness_version AS HarnessVersion";

    public required string Harness { get; init; }
    public required string SessionId { get; init; }

    /// <summary>
    /// Null when the row is unclaimed (<see cref="BindingKind"/> is
    /// <c>"none"</c>).
    /// </summary>
    public string? AgentName { get; init; }

    public required string BindingKind { get; init; }

    /// <summary>
    /// This Nitro instance's id, not the OS hostname.
    /// </summary>
    public required string Host { get; init; }

    public required string Cwd { get; init; }
    public required string WorkspacePath { get; init; }
    public required string EndpointKind { get; init; }

    /// <summary>
    /// Empty only when <see cref="EndpointKind"/> is <c>"none"</c>.
    /// </summary>
    public required string EndpointAddr { get; init; }

    /// <summary>
    /// The credential for endpoints that require one, or null when the
    /// endpoint has no credential.
    /// </summary>
    public string? EndpointSecret { get; init; }

    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset LastBeatAt { get; init; }
    public required int BlockBudgetUsed { get; init; }
    public DateTimeOffset? LastPingAt { get; init; }

    /// <summary>
    /// The most recent ping attempt id, or null before an attempt is claimed.
    /// Only results carrying the current attempt id are recorded.
    /// </summary>
    public string? LastPingAttempt { get; init; }

    /// <summary>
    /// The most recent completed ping result from <see cref="AgentPingResult"/>.
    /// Null before any attempt or while the latest attempt has no recorded result.
    /// </summary>
    public string? LastPingResult { get; init; }

    /// <summary>
    /// The optional ping diagnostic, limited to 200 characters.
    /// </summary>
    public string? LastPingDetail { get; init; }

    /// <summary>
    /// The normalized session role, or empty when no role is assigned.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The most recently recorded harness version, or empty when no version is recorded.
    /// </summary>
    public required string HarnessVersion { get; init; }
}

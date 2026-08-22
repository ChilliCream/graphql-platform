namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>agent_sessions</c>: one live harness session, claimed by an
/// agent or not. Presence has a lifetime of minutes, distinct from the
/// 30-day staleness semantics <see cref="AgentRecord"/> identity carries.
/// No command reads or writes this type yet - session claim/list/status and
/// the hook adapters land in a later bead; this shape exists so that work
/// has a column-matched row type to build on instead of ad hoc Dapper
/// projections.
/// </summary>
internal sealed record AgentSessionRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the agent_sessions table.
    /// </summary>
    public const string Columns =
        "harness AS Harness, session_id AS SessionId, agent_name AS AgentName, "
        + "binding_kind AS BindingKind, host AS Host, pid AS Pid, proc_start AS ProcStart, "
        + "cwd AS Cwd, workspace_path AS WorkspacePath, endpoint_kind AS EndpointKind, "
        + "endpoint_addr AS EndpointAddr, started_at AS StartedAt, last_beat_at AS LastBeatAt, "
        + "block_budget_used AS BlockBudgetUsed, last_ping_at AS LastPingAt, "
        + "last_ping_attempt AS LastPingAttempt, last_ping_result AS LastPingResult, "
        + "last_ping_detail AS LastPingDetail";

    public required string Harness { get; init; }
    public required string SessionId { get; init; }

    /// <summary>
    /// Null when the row is unclaimed (<see cref="BindingKind"/> is
    /// <c>"none"</c>).
    /// </summary>
    public string? AgentName { get; init; }

    public required string BindingKind { get; init; }

    /// <summary>
    /// This Nitro instance's id (see the schema v4 migration notes), not the
    /// OS hostname; pid liveness is only meaningful against the instance
    /// that spawned it.
    /// </summary>
    public required string Host { get; init; }

    public required int Pid { get; init; }

    /// <summary>
    /// The process's absolute start time (.NET <c>Process.StartTime</c>),
    /// immune to reboot pid collisions.
    /// </summary>
    public required DateTimeOffset ProcStart { get; init; }

    public required string Cwd { get; init; }
    public required string WorkspacePath { get; init; }
    public required string EndpointKind { get; init; }

    /// <summary>
    /// Empty only when <see cref="EndpointKind"/> is <c>"none"</c>.
    /// </summary>
    public required string EndpointAddr { get; init; }

    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset LastBeatAt { get; init; }
    public required int BlockBudgetUsed { get; init; }
    public DateTimeOffset? LastPingAt { get; init; }

    /// <summary>
    /// The attempt id of the most recent ping; results only write back if
    /// they carry this id, so an out-of-order completion cannot overwrite a
    /// newer attempt's result.
    /// </summary>
    public string? LastPingAttempt { get; init; }

    public string? LastPingResult { get; init; }

    /// <summary>
    /// An application-truncated diagnostic code (never raw stderr), at most
    /// 200 characters.
    /// </summary>
    public string? LastPingDetail { get; init; }
}

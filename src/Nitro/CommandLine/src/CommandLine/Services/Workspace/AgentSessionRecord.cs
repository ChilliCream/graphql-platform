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
        + "last_ping_detail AS LastPingDetail, role AS Role, harness_version AS HarnessVersion, "
        + "process_scope AS ProcessScope, proc_start_legacy AS ProcStartLegacy";

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
    /// The process's raw kernel start-tick count (see
    /// <see cref="ProcStat.ReadStartTicks(int)"/>), immune to reboot pid
    /// collisions, compared by exact string equality. When
    /// <see cref="ProcStartLegacy"/> is true, this instead carries the
    /// pre-v6 DateTimeOffset text a migration left in place until this
    /// row's next SessionStart rewrites it.
    /// </summary>
    public required string ProcStart { get; init; }

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

    /// <summary>
    /// One of <c>ok</c>, <c>spawn-failed</c>, <c>endpoint-gone</c>,
    /// <c>timeout</c>, <c>capacity-dropped</c>, <c>error</c>, or
    /// <c>unsupported</c> (an endpoint kind or protocol the notifier cannot
    /// transport). Null before any ping attempt.
    /// </summary>
    public string? LastPingResult { get; init; }

    /// <summary>
    /// An application-truncated diagnostic code (never raw stderr), at most
    /// 200 characters.
    /// </summary>
    public string? LastPingDetail { get; init; }

    /// <summary>
    /// The mutable participant role, normalized the way
    /// <see cref="AgentRole.Normalize"/> normalizes an agent's durable role.
    /// Blank until a caller promotes it.
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// The exact harness version, captured once for the row's lifetime.
    /// Blank until a caller captures it.
    /// </summary>
    public required string HarnessVersion { get; init; }

    /// <summary>
    /// The PID/boot namespace visibility scope the process that wrote this
    /// row observed (see <see cref="IProcessInfoProvider.GetProcessScope"/>),
    /// captured once for the row's lifetime. Blank on a platform or
    /// environment that exposes no such signal.
    /// </summary>
    public required string ProcessScope { get; init; }

    /// <summary>
    /// True when this row's <see cref="ProcStart"/> is still the pre-v6
    /// DateTimeOffset text a schema migration left in place rather than raw
    /// kernel start ticks, set explicitly by that migration and cleared to
    /// false the next time a SessionStart rewrites the row with a freshly
    /// observed generation. Callers checking liveness (<see
    /// cref="IProcessInfoProvider.Observe"/>) must pass this through so a
    /// legacy row falls back to the pre-migration wall-clock comparison
    /// instead of being compared as raw ticks. A generation-predicated
    /// mutation (claim, heartbeat, ping, delete) matches <c>proc_start</c>
    /// by SQL equality against raw ticks, so it no-ops on a legacy row until
    /// its next SessionStart rewrites it.
    /// </summary>
    public required bool ProcStartLegacy { get; init; }
}

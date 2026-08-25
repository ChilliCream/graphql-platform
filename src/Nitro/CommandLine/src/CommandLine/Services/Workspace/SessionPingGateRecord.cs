namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// A row of <c>session_ping_gates</c>: the mutual-exclusion reservation held
/// while a transport attempt is in flight against one exact
/// <see cref="AgentSessionGeneration"/>. See
/// <see cref="ISessionPingGateStore"/>.
/// </summary>
internal sealed record SessionPingGateRecord
{
    /// <summary>
    /// The column list matching this type's properties, for use in SELECT
    /// statements against the session_ping_gates table.
    /// </summary>
    public const string Columns =
        "harness AS Harness, session_id AS SessionId, host AS Host, pid AS Pid, proc_start AS ProcStart, "
        + "attempt_id AS AttemptId, acquired_at AS AcquiredAt, expires_at AS ExpiresAt";

    public required string Harness { get; init; }
    public required string SessionId { get; init; }
    public required string Host { get; init; }
    public required int Pid { get; init; }
    public required string ProcStart { get; init; }
    public required string AttemptId { get; init; }
    public required DateTimeOffset AcquiredAt { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}

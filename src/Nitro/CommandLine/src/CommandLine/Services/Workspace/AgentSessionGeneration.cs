namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The full generation identity every <c>agent_sessions</c> lifecycle
/// mutation predicates on: <c>(harness, session_id)</c> addresses the row,
/// and <c>(host, pid, proc_start)</c> pins it to the exact process instance
/// that owns it. A caller holding a stale generation (an older pid reused by
/// the OS, or a process that has since been superseded by a fresh
/// SessionStart) can never mutate or delete a row a newer generation now
/// owns, because the WHERE clause simply matches nothing.
/// </summary>
internal sealed record AgentSessionGeneration(
    string Harness, string SessionId, string Host, int Pid, DateTimeOffset ProcStart);

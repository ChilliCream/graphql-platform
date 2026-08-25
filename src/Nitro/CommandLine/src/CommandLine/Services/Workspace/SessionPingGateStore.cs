using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class SessionPingGateStore(IFileSystem fileSystem, AgentDatabase database) : ISessionPingGateStore
{
    public async Task<bool> TryAcquireAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // The DO UPDATE's WHERE clause is the atomic steal-if-expired check:
        // when it evaluates false (the gate is held by an unexpired
        // attempt), SQLite treats the whole upsert as a no-op and RETURNING
        // yields no row, so a null result here means "not claimed" without
        // this call ever needing to compare the returned attempt id against
        // its own.
        var claimed = await connection.QueryFirstOrDefaultAsync<string>(
            """
            INSERT INTO session_ping_gates (
                harness, session_id, host, pid, proc_start, attempt_id, acquired_at, expires_at
            )
            VALUES (
                @harness, @sessionId, @host, @pid, @procStart, @attemptId, @now, @expiresAt
            )
            ON CONFLICT (harness, session_id, host, pid, proc_start) DO UPDATE SET
                attempt_id = excluded.attempt_id,
                acquired_at = excluded.acquired_at,
                expires_at = excluded.expires_at
            WHERE session_ping_gates.expires_at <= @now
            RETURNING attempt_id
            """,
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                pid = generation.Pid,
                procStart = generation.ProcStart,
                attemptId,
                now,
                expiresAt = now + leaseDuration,
                cancellationToken
            });

        return claimed is not null;
    }

    public async Task<bool> TryRenewAsync(
        AgentSessionGeneration generation,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var renewed = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE session_ping_gates SET expires_at = @expiresAt
            WHERE harness = @harness AND session_id = @sessionId AND host = @host
              AND pid = @pid AND proc_start = @procStart
              AND attempt_id = @attemptId AND expires_at > @now
            RETURNING attempt_id
            """,
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                pid = generation.Pid,
                procStart = generation.ProcStart,
                attemptId,
                now,
                expiresAt = now + leaseDuration,
                cancellationToken
            });

        return renewed is not null;
    }

    public async Task ReleaseAsync(
        AgentSessionGeneration generation, string attemptId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            DELETE FROM session_ping_gates
            WHERE harness = @harness AND session_id = @sessionId AND host = @host
              AND pid = @pid AND proc_start = @procStart AND attempt_id = @attemptId
            """,
            new
            {
                harness = generation.Harness,
                sessionId = generation.SessionId,
                host = generation.Host,
                pid = generation.Pid,
                procStart = generation.ProcStart,
                attemptId,
                cancellationToken
            });
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

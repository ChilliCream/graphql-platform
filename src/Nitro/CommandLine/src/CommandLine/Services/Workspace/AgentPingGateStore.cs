using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class AgentPingGateStore(IFileSystem fileSystem, AgentDatabase database) : IAgentPingGateStore
{
    public async Task<bool> TryAcquireAsync(
        string agent,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // An unexpired gate remains claimed by its current attempt.
        var claimed = await connection.QueryFirstOrDefaultAsync<string>(
            """
            INSERT INTO agent_ping_gates (agent, attempt_id, acquired_at, expires_at)
            VALUES (@agent, @attemptId, @now, @expiresAt)
            ON CONFLICT (agent) DO UPDATE SET
                attempt_id = excluded.attempt_id,
                acquired_at = excluded.acquired_at,
                expires_at = excluded.expires_at
            WHERE agent_ping_gates.expires_at <= @now
            RETURNING attempt_id
            """,
            new { agent, attemptId, now, expiresAt = now + leaseDuration, cancellationToken });

        return claimed is not null;
    }

    public async Task<bool> TryRenewAsync(
        string agent,
        string attemptId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var renewed = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE agent_ping_gates SET expires_at = @expiresAt
            WHERE agent = @agent AND attempt_id = @attemptId AND expires_at > @now
            RETURNING attempt_id
            """,
            new { agent, attemptId, now, expiresAt = now + leaseDuration, cancellationToken });

        return renewed is not null;
    }

    public async Task ReleaseAsync(string agent, string attemptId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            DELETE FROM agent_ping_gates WHERE agent = @agent AND attempt_id = @attemptId
            """,
            new { agent, attemptId, cancellationToken });
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

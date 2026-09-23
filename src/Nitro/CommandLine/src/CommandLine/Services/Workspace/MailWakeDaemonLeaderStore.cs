using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class MailWakeDaemonLeaderStore(
    IFileSystem fileSystem, AgentDatabase database) : IMailWakeDaemonLeaderStore
{
    public async Task<bool> TryAcquireAsync(
        string token,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var acquired = await connection.QueryFirstOrDefaultAsync<string>(
            """
            INSERT INTO mail_wake_daemons (id, owner_token, acquired_at, heartbeat_at, expires_at)
            VALUES (1, @token, @now, @now, @expiresAt)
            ON CONFLICT (id) DO UPDATE SET
                owner_token = excluded.owner_token,
                acquired_at = excluded.acquired_at,
                heartbeat_at = excluded.heartbeat_at,
                expires_at = excluded.expires_at
            WHERE mail_wake_daemons.expires_at <= @now
            RETURNING owner_token
            """,
            new { token, now, expiresAt = now + leaseDuration, cancellationToken });

        return acquired is not null;
    }

    public async Task<bool> TryRenewAsync(
        string token,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var renewed = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE mail_wake_daemons SET expires_at = @expiresAt, heartbeat_at = @now
            WHERE id = 1 AND owner_token = @token AND expires_at > @now
            RETURNING owner_token
            """,
            new { token, now, expiresAt = now + leaseDuration, cancellationToken });

        return renewed is not null;
    }

    public async Task<bool> TryReleaseAsync(string token, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var released = await connection.QueryFirstOrDefaultAsync<string>(
            """
            UPDATE mail_wake_daemons SET expires_at = @now
            WHERE id = 1 AND owner_token = @token
            RETURNING owner_token
            """,
            new { token, now, cancellationToken });

        return released is not null;
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

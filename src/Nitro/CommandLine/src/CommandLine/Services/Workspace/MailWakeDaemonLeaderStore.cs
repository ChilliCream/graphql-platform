using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class MailWakeDaemonLeaderStore(
    IFileSystem fileSystem, AgentDatabase database) : IMailWakeDaemonLeaderStore
{
    public async Task<long?> TryAcquireAsync(
        string nitroInstanceId,
        string ownerId,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        // Same steal-if-expired shape as SessionPingGateStore.TryAcquireAsync:
        // the DO UPDATE's WHERE clause makes a live, unexpired lease a
        // complete no-op, so RETURNING yields no row and this call returns
        // null without needing to inspect who currently holds it.
        return await connection.QueryFirstOrDefaultAsync<long?>(
            """
            INSERT INTO mail_wake_daemons (nitro_instance_id, owner_id, epoch, leased_at, expires_at)
            VALUES (@nitroInstanceId, @ownerId, 1, @now, @expiresAt)
            ON CONFLICT (nitro_instance_id) DO UPDATE SET
                owner_id = excluded.owner_id,
                epoch = mail_wake_daemons.epoch + 1,
                leased_at = excluded.leased_at,
                expires_at = excluded.expires_at
            WHERE mail_wake_daemons.expires_at <= @now
            RETURNING epoch
            """,
            new { nitroInstanceId, ownerId, now, expiresAt = now + leaseDuration, cancellationToken });
    }

    public async Task<bool> TryRenewAsync(
        string nitroInstanceId,
        string ownerId,
        long epoch,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        string? lastError,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var renewedEpoch = await connection.QueryFirstOrDefaultAsync<long?>(
            """
            UPDATE mail_wake_daemons SET expires_at = @expiresAt, leased_at = @now, last_error = @lastError
            WHERE nitro_instance_id = @nitroInstanceId AND owner_id = @ownerId AND epoch = @epoch
              AND expires_at > @now
            RETURNING epoch
            """,
            new
            {
                nitroInstanceId,
                ownerId,
                epoch,
                now,
                expiresAt = now + leaseDuration,
                lastError,
                cancellationToken
            });

        return renewedEpoch is not null;
    }

    public async Task<bool> TryReleaseAsync(
        string nitroInstanceId,
        string ownerId,
        long epoch,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var releasedEpoch = await connection.QueryFirstOrDefaultAsync<long?>(
            """
            UPDATE mail_wake_daemons SET expires_at = @now
            WHERE nitro_instance_id = @nitroInstanceId AND owner_id = @ownerId AND epoch = @epoch
            RETURNING epoch
            """,
            new { nitroInstanceId, ownerId, epoch, now, cancellationToken });

        return releasedEpoch is not null;
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

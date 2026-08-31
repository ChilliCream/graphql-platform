using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class PingLeaseStore(IFileSystem fileSystem, AgentDatabase database) : IPingLeaseStore
{
    public async Task<int?> TryAcquireAsync(
        string attemptId, DateTimeOffset now, TimeSpan leaseDuration, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Stealing an expired lease's slot happens by simply deleting it
        // before looking for a free slot: every caller's hard timeout
        // bounds its own attempt's digest and transport work against an
        // absolute deadline fixed once at lease acquisition, keeping a
        // margin under the lease duration it acquired, so a lease past its
        // own `expires_at` is expected to already be released.
        await using (var expireCommand = connection.CreateCommand())
        {
            expireCommand.Transaction = (SqliteTransaction)transaction;
            expireCommand.CommandText = "DELETE FROM ping_leases WHERE expires_at <= @now;";
            expireCommand.Parameters.AddWithValue("@now", now);
            await expireCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        int? freeSlot = null;

        await using (var findCommand = connection.CreateCommand())
        {
            findCommand.Transaction = (SqliteTransaction)transaction;
            findCommand.CommandText =
                """
                SELECT column1 FROM (VALUES (1), (2), (3), (4))
                WHERE column1 NOT IN (SELECT slot FROM ping_leases)
                ORDER BY column1
                LIMIT 1;
                """;

            var result = await findCommand.ExecuteScalarAsync(cancellationToken);

            if (result is long slot)
            {
                freeSlot = (int)slot;
            }
        }

        if (freeSlot is null)
        {
            // Every slot is held by an unexpired lease: capacity-dropped,
            // the caller's job to record.
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        await using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.Transaction = (SqliteTransaction)transaction;
            insertCommand.CommandText =
                """
                INSERT INTO ping_leases (slot, attempt_id, acquired_at, expires_at)
                VALUES (@slot, @attemptId, @acquiredAt, @expiresAt);
                """;
            insertCommand.Parameters.AddWithValue("@slot", freeSlot.Value);
            insertCommand.Parameters.AddWithValue("@attemptId", attemptId);
            insertCommand.Parameters.AddWithValue("@acquiredAt", now);
            insertCommand.Parameters.AddWithValue("@expiresAt", now + leaseDuration);

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return freeSlot;
    }

    public async Task ReleaseAsync(int slot, string attemptId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM ping_leases WHERE slot = @slot AND attempt_id = @attemptId;";
        command.Parameters.AddWithValue("@slot", slot);
        command.Parameters.AddWithValue("@attemptId", attemptId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

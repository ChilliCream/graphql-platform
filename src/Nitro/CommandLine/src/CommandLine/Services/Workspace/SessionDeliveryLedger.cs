using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class SessionDeliveryLedger(IFileSystem fileSystem, AgentDatabase database) : ISessionDeliveryLedger
{
    public async Task<IReadOnlyList<string>> ReserveAsync(
        string harness,
        string sessionId,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reserved = new List<string>(messageIds.Count);

        foreach (var messageId in messageIds)
        {
            // ON CONFLICT DO NOTHING is the atomic claim: a zero row count
            // means this (harness, session_id, message_id, channel) was
            // already reserved, by this call's own session or an earlier
            // one, so the message is excluded rather than reserved twice.
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText =
                """
                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES (@harness, @sessionId, @messageId, @channel, @deliveredAt)
                ON CONFLICT DO NOTHING;
                """;
            command.Parameters.AddWithValue("@harness", harness);
            command.Parameters.AddWithValue("@sessionId", sessionId);
            command.Parameters.AddWithValue("@messageId", messageId);
            command.Parameters.AddWithValue("@channel", channel);
            command.Parameters.AddWithValue("@deliveredAt", deliveredAt);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected > 0)
            {
                reserved.Add(messageId);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return reserved;
    }

    public async Task<IReadOnlyList<string>> ReserveAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reserved = new List<string>(messageIds.Count);

        foreach (var messageId in messageIds)
        {
            // ON CONFLICT DO NOTHING is the atomic claim: a zero row count
            // means this (harness, session_id, message_id, channel) was
            // already reserved, by this call's own session or an earlier
            // one, so the message is excluded rather than reserved twice.
            // The EXISTS guard additionally confines the reservation to the
            // exact host that owns the session row, so a session recorded
            // by a different host (or already deleted) never reserves.
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText =
                """
                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                SELECT @harness, @sessionId, @messageId, @channel, @deliveredAt
                WHERE EXISTS (
                    SELECT 1 FROM agent_sessions
                    WHERE harness = @harness AND session_id = @sessionId AND host = @host)
                ON CONFLICT DO NOTHING;
                """;
            command.Parameters.AddWithValue("@harness", generation.Harness);
            command.Parameters.AddWithValue("@sessionId", generation.SessionId);
            command.Parameters.AddWithValue("@host", generation.Host);
            command.Parameters.AddWithValue("@messageId", messageId);
            command.Parameters.AddWithValue("@channel", channel);
            command.Parameters.AddWithValue("@deliveredAt", deliveredAt);

            var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

            if (rowsAffected > 0)
            {
                reserved.Add(messageId);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return reserved;
    }
}

using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class SessionDeliveryLedger(IFileSystem fileSystem, AgentDatabase database) : ISessionDeliveryLedger
{
    public async Task<IReadOnlyList<string>> FindDeliveredAsync(
        AgentSessionGeneration generation,
        IReadOnlyList<string> messageIds,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT message_id
            FROM session_deliveries
            WHERE harness = @harness AND session_id = @sessionId AND message_id IN (
            """
            + string.Join(", ", messageIds.Select((_, index) => $"@messageId{index}"))
            + ");";
        command.Parameters.AddWithValue("@harness", generation.Harness);
        command.Parameters.AddWithValue("@sessionId", generation.SessionId);

        for (var i = 0; i < messageIds.Count; i++)
        {
            command.Parameters.AddWithValue($"@messageId{i}", messageIds[i]);
        }

        var delivered = new HashSet<string>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            delivered.Add(reader.GetString(0));
        }

        return messageIds.Where(delivered.Contains).ToArray();
    }

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
}

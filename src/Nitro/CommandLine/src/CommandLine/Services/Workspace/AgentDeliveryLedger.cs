using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class AgentDeliveryLedger(IFileSystem fileSystem, AgentDatabase database) : IAgentDeliveryLedger
{
    public async Task<IReadOnlyList<string>> FindDeliveredAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        await using var connection = await ConnectAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT DISTINCT message_id
            FROM agent_deliveries
            WHERE agent = @agent AND message_id IN (
            """
            + string.Join(", ", messageIds.Select((_, index) => $"@messageId{index}"))
            + ");";
        command.Parameters.AddWithValue("@agent", agent);

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
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        DateTimeOffset deliveredAt,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return [];
        }

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var reserved = new List<string>(messageIds.Count);

        foreach (var messageId in messageIds)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText =
                """
                INSERT INTO agent_deliveries (agent, message_id, channel, delivered_at)
                VALUES (@agent, @messageId, @channel, @deliveredAt)
                ON CONFLICT DO NOTHING;
                """;
            command.Parameters.AddWithValue("@agent", agent);
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

    public async Task ReleaseAsync(
        string agent,
        IReadOnlyList<string> messageIds,
        string channel,
        CancellationToken cancellationToken)
    {
        if (messageIds.Count == 0)
        {
            return;
        }

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var messageId in messageIds)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = (SqliteTransaction)transaction;
            command.CommandText =
                """
                DELETE FROM agent_deliveries
                WHERE agent = @agent AND message_id = @messageId AND channel = @channel;
                """;
            command.Parameters.AddWithValue("@agent", agent);
            command.Parameters.AddWithValue("@messageId", messageId);
            command.Parameters.AddWithValue("@channel", channel);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

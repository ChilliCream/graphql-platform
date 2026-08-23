using Dapper;

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
            var rowsAffected = await connection.ExecuteAsync(
                """
                INSERT INTO session_deliveries (harness, session_id, message_id, channel, delivered_at)
                VALUES (@harness, @sessionId, @messageId, @channel, @deliveredAt)
                ON CONFLICT DO NOTHING;
                """,
                new { harness, sessionId, messageId, channel, deliveredAt, cancellationToken },
                transaction);

            if (rowsAffected > 0)
            {
                reserved.Add(messageId);
            }
        }

        await transaction.CommitAsync(cancellationToken);

        return reserved;
    }
}

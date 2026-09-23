using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Mail;

internal sealed class MailStore(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database,
    IAgentRegistry agentRegistry,
    INitroInstanceIdProvider? instanceIdProvider = null,
    IGlobalConfigDirectoryProvider? globalConfigDirectoryProvider = null) : IMailStore
{
    private const string IdPrefix = "m-";
    private const string IdAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int MinIdLength = 6;
    private const int MaxIdAttempts = 10;

    /// <summary>
    /// Initializes the workspace database and returns an open connection owned by the caller.
    /// </summary>
    public async Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
        => await database.InitializeAsync(workspaceDirectory, cancellationToken);

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindWorkspaceDirectory()
            ?? throw new ExitException(
                "No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    public string? FindWorkspaceDirectory()
        => AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

    public async Task InitializeWorkspaceAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        await using var connection = await InitializeAsync(workspaceDirectory, cancellationToken);
    }

    public async Task<MailMessage> SendMessageAsync(
        MailMessageCreation creation,
        CancellationToken cancellationToken)
    {
        var sender = MailAgentName.Normalize(creation.Sender);
        var subject = creation.Subject.Trim();

        if (subject.Length is 0 or > 500)
        {
            throw new ExitException("A message subject must be between 1 and 500 characters.");
        }

        var recipients = BuildRecipients(creation.To, creation.Cc);

        if (recipients.Count == 0)
        {
            throw new ExitException("A message must have at least one recipient.");
        }

        var now = timeProvider.GetUtcNow();
        var seed = $"{sender}|{subject}|{now:O}";

        // Registers the sender before the message transaction.
        await agentRegistry.TouchAsync(sender, cancellationToken);

        // Creates missing recipient identities before the message transaction.
        var unregistered = await EnsureRecipientsAsync(recipients, cancellationToken);

        // Resolves wake ownership before the message transaction.
        var nitroInstanceId = creation.WakePolicy == MailWakePolicy.Enqueue
            ? await ResolveNitroInstanceIdAsync(cancellationToken)
            : null;

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var id = await CreateMessageIdAsync(connection, seed, cancellationToken, transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO messages (
                id,
                thread_id,
                in_reply_to,
                sender,
                subject,
                body,
                created_at
            )
            VALUES (
                @Id,
                @ThreadId,
                @InReplyTo,
                @Sender,
                @Subject,
                @Body,
                @CreatedAt
            )
            """,
            new
            {
                Id = id,
                ThreadId = id,
                InReplyTo = (string?)null,
                Sender = sender,
                Subject = subject,
                Body = creation.Body,
                CreatedAt = now,
                cancellationToken
            },
            transaction);

        await InsertRecipientsAsync(connection, id, recipients, cancellationToken, transaction);

        var wakeReceipts = nitroInstanceId is null
            ? []
            : await EnqueueWakeAsync(connection, nitroInstanceId, recipients, now, cancellationToken, transaction);

        await transaction.CommitAsync(cancellationToken);

        return new MailMessage
        {
            Id = id,
            ThreadId = id,
            InReplyTo = null,
            Sender = sender,
            Subject = subject,
            Body = creation.Body,
            CreatedAt = now,
            Recipients = recipients,
            Unregistered = unregistered,
            WakeReceipts = wakeReceipts
        };
    }

    public async Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        CancellationToken cancellationToken)
        => await ReplyMessageAsync(inReplyToId, sender, body, MailWakePolicy.Skip, cancellationToken);

    public async Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        MailWakePolicy wakePolicy,
        CancellationToken cancellationToken)
    {
        var actor = MailAgentName.Normalize(sender);
        var now = timeProvider.GetUtcNow();

        var (original, root, recipients) =
            await ResolveReplyAsync(inReplyToId, actor, cancellationToken);

        // Registers the replying actor after participant validation.
        await agentRegistry.TouchAsync(actor, cancellationToken);

        // Resolves wake ownership before the reply transaction.
        var nitroInstanceId = wakePolicy == MailWakePolicy.Enqueue
            ? await ResolveNitroInstanceIdAsync(cancellationToken)
            : null;

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var seed = $"{actor}|{root.Subject}|{now:O}";
        var id = await CreateMessageIdAsync(connection, seed, cancellationToken, transaction);

        await connection.ExecuteAsync(
            """
            INSERT INTO messages (
                id,
                thread_id,
                in_reply_to,
                sender,
                subject,
                body,
                created_at
            )
            VALUES (
                @Id,
                @ThreadId,
                @InReplyTo,
                @Sender,
                @Subject,
                @Body,
                @CreatedAt
            )
            """,
            new
            {
                Id = id,
                ThreadId = original.ThreadId,
                InReplyTo = original.Id,
                Sender = actor,
                Subject = root.Subject,
                Body = body,
                CreatedAt = now,
                cancellationToken
            },
            transaction);

        await InsertRecipientsAsync(connection, id, recipients, cancellationToken, transaction);

        var wakeReceipts = nitroInstanceId is null
            ? []
            : await EnqueueWakeAsync(connection, nitroInstanceId, recipients, now, cancellationToken, transaction);

        await transaction.CommitAsync(cancellationToken);

        return new MailMessage
        {
            Id = id,
            ThreadId = original.ThreadId,
            InReplyTo = original.Id,
            Sender = actor,
            Subject = root.Subject,
            Body = body,
            CreatedAt = now,
            Recipients = recipients,
            WakeReceipts = wakeReceipts
        };
    }

    public async Task<MailTransferResult> TransferParticipationAsync(
        string from,
        string to,
        CancellationToken cancellationToken)
    {
        var source = MailAgentName.Normalize(from);
        var target = MailAgentName.Normalize(to);

        if (source == target)
        {
            throw new ExitException("The source and target agents must be different.");
        }

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var targetExists = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM agents WHERE name = @target",
            new { target, cancellationToken },
            transaction);

        if (targetExists == 0)
        {
            throw new ExitException($"Target agent '{target}' does not exist.");
        }

        // Only unread, unarchived recipient rows move; read or archived mail stays
        // with the source, and a message's sender is never rewritten.
        var dropped = await connection.ExecuteAsync(
            """
            DELETE FROM message_recipients AS source
            WHERE source.recipient = @source
                AND source.read_at IS NULL
                AND source.archived_at IS NULL
                AND EXISTS (
                    SELECT 1
                    FROM message_recipients AS target
                    WHERE target.message_id = source.message_id
                        AND target.recipient = @target)
            """,
            new { source, target, cancellationToken },
            transaction);

        var recipientMessageIds = (await connection.QueryAsync<string>(
            """
            SELECT message_id FROM message_recipients
            WHERE recipient = @source AND read_at IS NULL AND archived_at IS NULL
            ORDER BY message_id
            """,
            new { source, cancellationToken },
            transaction)).ToArray();

        var recipientsMoved = await connection.ExecuteAsync(
            """
            UPDATE message_recipients
            SET recipient = @target
            WHERE recipient = @source
                AND read_at IS NULL
                AND archived_at IS NULL
            """,
            new { source, target, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return new MailTransferResult(recipientsMoved, dropped)
        {
            RecipientMessageIds = recipientMessageIds
        };
    }

    /// <summary>
    /// Resolves this machine's Nitro instance id for a
    /// <see cref="MailWakePolicy.Enqueue"/> send or reply. Throws
    /// <see cref="InvalidOperationException"/> when this store was
    /// constructed without the instance id and global config directory
    /// providers <see cref="MailWakePolicy.Enqueue"/> requires.
    /// </summary>
    private async Task<string> ResolveNitroInstanceIdAsync(CancellationToken cancellationToken)
    {
        if (instanceIdProvider is null || globalConfigDirectoryProvider is null)
        {
            throw new InvalidOperationException(
                "MailWakePolicy.Enqueue requires this MailStore to be constructed with an "
                + "INitroInstanceIdProvider and an IGlobalConfigDirectoryProvider.");
        }

        return await instanceIdProvider.GetIdAsync(globalConfigDirectoryProvider.GetDirectory(), cancellationToken);
    }

    /// <summary>
    /// Advances each recipient's wake generation for the Nitro instance and returns
    /// the resulting tokens in recipient order. Preserves the earlier due time
    /// when wake work already exists.
    /// </summary>
    private static async Task<List<MailWakeReceipt>> EnqueueWakeAsync(
        SqliteConnection connection,
        string nitroInstanceId,
        IReadOnlyList<MailRecipient> recipients,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        DbTransaction transaction)
    {
        var receipts = new List<MailWakeReceipt>(recipients.Count);

        foreach (var recipient in recipients)
        {
            var generation = await connection.QueryFirstOrDefaultAsync<long>(
                """
                INSERT INTO mail_wake_outbox (
                    nitro_instance_id, actor, requested_generation, settled_generation, due_at, updated_at
                )
                VALUES (@nitroInstanceId, @actor, 1, 0, @now, @now)
                ON CONFLICT (nitro_instance_id, actor) DO UPDATE SET
                    requested_generation = requested_generation + 1,
                    due_at = MIN(due_at, excluded.due_at),
                    updated_at = excluded.updated_at
                RETURNING requested_generation
                """,
                new { nitroInstanceId, actor = recipient.Name, now, cancellationToken },
                transaction);

            receipts.Add(new MailWakeReceipt { Actor = recipient.Name, Generation = generation });
        }

        return receipts;
    }

    /// <summary>
    /// Returns the original message, thread root, and computed reply recipients.
    /// Throws <see cref="ExitException"/> when the message is missing, the actor is not
    /// a participant, or no recipients remain.
    /// </summary>
    private async Task<(MailMessage Original, MailMessage Root, List<MailRecipient> Recipients)> ResolveReplyAsync(
        string inReplyToId,
        string actor,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var original = await GetMessageAsync(connection, inReplyToId, cancellationToken)
            ?? throw new ExitException($"Message '{inReplyToId}' does not exist.");

        var participants = new HashSet<string>(StringComparer.Ordinal) { original.Sender };

        foreach (var recipient in original.Recipients)
        {
            participants.Add(recipient.Name);
        }

        if (!participants.Contains(actor))
        {
            throw new ExitException(
                $"'{actor}' is not the sender or a recipient of '{inReplyToId}' and cannot reply to it.");
        }

        var candidates = new List<string> { original.Sender };
        candidates.AddRange(original.Recipients.OrderBy(r => r.Ordinal).Select(r => r.Name));

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var recipients = new List<MailRecipient>();

        foreach (var name in candidates)
        {
            if (name == actor || !seen.Add(name))
            {
                continue;
            }

            recipients.Add(new MailRecipient
            {
                Name = name,
                Kind = MailRecipientKinds.To,
                Ordinal = recipients.Count
            });
        }

        if (recipients.Count == 0)
        {
            throw new ExitException(
                $"Replying to '{inReplyToId}' as '{actor}' would leave no recipients.");
        }

        var root = await GetMessageAsync(connection, original.ThreadId, cancellationToken)
            ?? original;

        return (original, root, recipients);
    }

    public async Task<MailMessage?> GetMessageAsync(
        string id,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await GetMessageAsync(connection, id, cancellationToken);
    }

    public async Task<MailMessage> GetRequiredMessageAsync(
        string id,
        CancellationToken cancellationToken)
        => await GetMessageAsync(id, cancellationToken)
            ?? throw new ExitException($"Message '{id}' does not exist.");

    private static async Task<MailMessage?> GetMessageAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        var row = await connection.QueryFirstOrDefaultAsync<MailMessageRow>(
            $"SELECT {MailMessage.Columns} FROM messages WHERE id = @id",
            new { id, cancellationToken },
            transaction);

        if (row is null)
        {
            return null;
        }

        var recipients = await GetRecipientsAsync(connection, id, cancellationToken, transaction);

        return row.ToMailMessage(recipients);
    }

    private static async Task<IReadOnlyList<MailRecipient>> GetRecipientsAsync(
        SqliteConnection connection,
        string messageId,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        var rows = await connection.QueryAsync<MailRecipientRow>(
            $"""
            SELECT {MailRecipient.Columns} FROM message_recipients
            WHERE message_id = @messageId
            ORDER BY ordinal
            """,
            new { messageId, cancellationToken },
            transaction);

        return rows.Select(r => r.ToMailRecipient()).ToList();
    }

    public async Task<IReadOnlyList<MailMessage>> GetThreadMessagesAsync(
        string threadId,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<MailMessageRow>(
            $"""
            SELECT {MailMessage.Columns} FROM messages
            WHERE thread_id = @threadId
            ORDER BY created_at, id
            """,
            new { threadId, cancellationToken });

        var messages = new List<MailMessage>();

        foreach (var row in rows)
        {
            var recipients = await GetRecipientsAsync(connection, row.Id, cancellationToken);
            messages.Add(row.ToMailMessage(recipients));
        }

        return messages;
    }

    public async Task<IReadOnlyList<MailMessage>> QueryInboxAsync(
        MailInboxFilter filter,
        CancellationToken cancellationToken)
    {
        var normalizedFilter = filter with
        {
            Actor = MailAgentName.Normalize(filter.Actor),
            From = string.IsNullOrEmpty(filter.From) ? null : MailAgentName.Normalize(filter.From)
        };

        var (sql, parameters) = BuildInboxQuery(normalizedFilter);

        await using var connection = await ConnectAsync(cancellationToken);

        var ids = await ExecuteIdQueryAsync(connection, sql, parameters, cancellationToken);
        var messages = new List<MailMessage>(ids.Count);

        foreach (var id in ids)
        {
            var message = await GetMessageAsync(connection, id, cancellationToken);

            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private static (string Sql, Dictionary<string, object?> Parameters) BuildInboxQuery(
        MailInboxFilter filter)
    {
        var conditions = new List<string> { "mr.recipient = @actor" };
        var parameters = new Dictionary<string, object?> { ["actor"] = filter.Actor };

        if (filter.UnreadOnly)
        {
            conditions.Add("mr.read_at IS NULL");
        }

        if (!filter.IncludeArchived)
        {
            conditions.Add("mr.archived_at IS NULL");
        }

        if (filter.From is not null)
        {
            parameters["from"] = filter.From;
            conditions.Add("m.sender = @from");
        }

        if (filter.Since is { } since)
        {
            parameters["since"] = since;
            conditions.Add("m.created_at >= @since");
        }

        var sql =
            $"""
            SELECT m.id
            FROM messages m
            JOIN message_recipients mr ON mr.message_id = m.id
            WHERE {string.Join(" AND ", conditions)}
            ORDER BY m.created_at DESC, m.id DESC
            """;

        if (filter.Limit is { } limit)
        {
            parameters["limit"] = limit;
            sql += " LIMIT @limit";
        }

        return (sql, parameters);
    }

    public async Task<IReadOnlyList<MailMessage>> QueryWorkspaceMessagesAsync(
        MailWorkspaceFilter filter,
        CancellationToken cancellationToken)
    {
        var normalizedFilter = filter with
        {
            Agent = string.IsNullOrEmpty(filter.Agent) ? null : MailAgentName.Normalize(filter.Agent)
        };

        var (sql, parameters) = BuildWorkspaceQuery(normalizedFilter);

        await using var connection = await ConnectAsync(cancellationToken);

        var ids = await ExecuteIdQueryAsync(connection, sql, parameters, cancellationToken);
        var messages = new List<MailMessage>(ids.Count);

        foreach (var id in ids)
        {
            var message = await GetMessageAsync(connection, id, cancellationToken);

            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    private static (string Sql, Dictionary<string, object?> Parameters) BuildWorkspaceQuery(
        MailWorkspaceFilter filter)
    {
        var conditions = new List<string>();
        var parameters = new Dictionary<string, object?>();
        var joinRecipients = filter.Agent is not null;

        if (filter.Agent is { } agent)
        {
            parameters["agent"] = agent;
            conditions.Add("(m.sender = @agent OR mr.recipient = @agent)");
        }

        if (filter.Since is { } since)
        {
            parameters["since"] = since;
            conditions.Add("m.created_at >= @since");
        }

        var select = joinRecipients ? "SELECT DISTINCT m.id" : "SELECT m.id";
        var from = joinRecipients
            ? "FROM messages m JOIN message_recipients mr ON mr.message_id = m.id"
            : "FROM messages m";
        var where = conditions.Count > 0 ? $"WHERE {string.Join(" AND ", conditions)}" : "";

        var sql =
            $"""
            {select}
            {from}
            {where}
            ORDER BY m.created_at DESC, m.id DESC
            """;

        if (filter.Limit is { } limit)
        {
            parameters["limit"] = limit;
            sql += " LIMIT @limit";
        }

        return (sql, parameters);
    }

    private static async Task<List<string>> ExecuteIdQueryAsync(
        SqliteConnection connection,
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue("@" + name, value ?? DBNull.Value);
        }

        var results = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(reader.GetString(0));
        }

        return results;
    }

    public async Task MarkReadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ValidateRecipientOwnershipAsync(
            connection, messageIds, normalizedActor, cancellationToken, transaction);

        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(
                """
                UPDATE message_recipients
                SET read_at = @readAt
                WHERE message_id = @messageId AND recipient = @recipient
                """,
                new
                {
                    readAt = now,
                    messageId,
                    recipient = normalizedActor,
                    cancellationToken
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task MarkUnreadAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ValidateRecipientOwnershipAsync(
            connection, messageIds, normalizedActor, cancellationToken, transaction);

        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(
                """
                UPDATE message_recipients
                SET read_at = NULL
                WHERE message_id = @messageId AND recipient = @recipient
                """,
                new { messageId, recipient = normalizedActor, cancellationToken },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await ValidateRecipientOwnershipAsync(
            connection, messageIds, normalizedActor, cancellationToken, transaction);

        foreach (var messageId in messageIds)
        {
            await connection.ExecuteAsync(
                """
                UPDATE message_recipients
                SET archived_at = @archivedAt
                WHERE message_id = @messageId AND recipient = @recipient
                """,
                new
                {
                    archivedAt = now,
                    messageId,
                    recipient = normalizedActor,
                    cancellationToken
                },
                transaction);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    // Validates every recipient copy before the caller changes any of them.
    private static async Task ValidateRecipientOwnershipAsync(
        SqliteConnection connection,
        IReadOnlyList<string> messageIds,
        string actor,
        CancellationToken cancellationToken,
        DbTransaction transaction)
    {
        var notAddressed = new List<string>();

        foreach (var messageId in messageIds)
        {
            var exists = await connection.ExecuteScalarAsync<long>(
                """
                SELECT COUNT(*) FROM message_recipients
                WHERE message_id = @messageId AND recipient = @recipient
                """,
                new { messageId, recipient = actor, cancellationToken },
                transaction);

            if (exists == 0)
            {
                notAddressed.Add(messageId);
            }
        }

        if (notAddressed.Count > 0)
        {
            throw new ExitException(
                $"'{actor}' is not a recipient of: {string.Join(", ", notAddressed)}.");
        }
    }

    public async Task<IReadOnlyList<MailThreadSummary>> QueryThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await using var connection = await ConnectAsync(cancellationToken);

        var rollups = await connection.QueryAsync<ThreadRollupRow>(
            """
            SELECT
                m.thread_id AS ThreadId,
                COUNT(*) AS MessageCount,
                MAX(m.created_at) AS LastMessageAt
            FROM messages m
            WHERE m.thread_id IN (
                SELECT thread_id FROM messages WHERE sender = @actor
                UNION
                SELECT m2.thread_id
                FROM messages m2
                JOIN message_recipients mr ON mr.message_id = m2.id
                WHERE mr.recipient = @actor
            )
            GROUP BY m.thread_id
            """,
            new { actor = normalizedActor, cancellationToken });

        return await BuildThreadSummariesAsync(connection, rollups, normalizedActor, preserveOrder: false, cancellationToken);
    }

    public async Task<IReadOnlyList<MailThreadSummary>> QueryInboxThreadsAsync(
        string actor,
        bool includeArchived,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await using var connection = await ConnectAsync(cancellationToken);

        var rollups = includeArchived
            ? await connection.QueryAsync<ThreadRollupRow>(
                """
                SELECT
                    m.thread_id AS ThreadId,
                    COUNT(*) AS MessageCount,
                    MAX(m.created_at) AS LastMessageAt
                FROM messages m
                WHERE m.thread_id IN (
                    SELECT DISTINCT m2.thread_id
                    FROM messages m2
                    JOIN message_recipients mr ON mr.message_id = m2.id
                    WHERE mr.recipient = @actor
                )
                GROUP BY m.thread_id
                """,
                new { actor = normalizedActor, cancellationToken })
            : await connection.QueryAsync<ThreadRollupRow>(
                """
                SELECT
                    m.thread_id AS ThreadId,
                    COUNT(*) AS MessageCount,
                    MAX(m.created_at) AS LastMessageAt
                FROM messages m
                WHERE m.thread_id IN (
                    SELECT DISTINCT m2.thread_id
                    FROM messages m2
                    JOIN message_recipients mr ON mr.message_id = m2.id
                    WHERE mr.recipient = @actor AND mr.archived_at IS NULL
                )
                GROUP BY m.thread_id
                """,
                new { actor = normalizedActor, cancellationToken });

        return await BuildThreadSummariesAsync(connection, rollups, normalizedActor, preserveOrder: false, cancellationToken);
    }

    public async Task<IReadOnlyList<MailThreadSummary>> QuerySentThreadsAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await using var connection = await ConnectAsync(cancellationToken);

        var rollups = await connection.QueryAsync<ThreadRollupRow>(
            """
            SELECT
                m.thread_id AS ThreadId,
                COUNT(*) AS MessageCount,
                MAX(m.created_at) AS LastMessageAt
            FROM messages m
            WHERE m.thread_id IN (
                SELECT thread_id FROM messages WHERE sender = @actor
            )
            GROUP BY m.thread_id
            """,
            new { actor = normalizedActor, cancellationToken });

        return await BuildThreadSummariesAsync(connection, rollups, normalizedActor, preserveOrder: false, cancellationToken);
    }

    public async Task<IReadOnlyList<MailThreadSummary>> QueryWorkspaceThreadsAsync(
        string? agent,
        CancellationToken cancellationToken)
    {
        var normalizedAgent = string.IsNullOrEmpty(agent) ? null : MailAgentName.Normalize(agent);

        await using var connection = await ConnectAsync(cancellationToken);

        var rollups = normalizedAgent is null
            ? await connection.QueryAsync<ThreadRollupRow>(
                new CommandDefinition(
                    """
                    SELECT
                        thread_id AS ThreadId,
                        COUNT(*) AS MessageCount,
                        MAX(created_at) AS LastMessageAt
                    FROM messages
                    GROUP BY thread_id
                    """,
                    cancellationToken: cancellationToken))
            : await connection.QueryAsync<ThreadRollupRow>(
                """
                SELECT
                    m.thread_id AS ThreadId,
                    COUNT(*) AS MessageCount,
                    MAX(m.created_at) AS LastMessageAt
                FROM messages m
                WHERE m.thread_id IN (
                    SELECT thread_id FROM messages WHERE sender = @agent
                    UNION
                    SELECT m2.thread_id
                    FROM messages m2
                    JOIN message_recipients mr ON mr.message_id = m2.id
                    WHERE mr.recipient = @agent
                )
                GROUP BY m.thread_id
                """,
                new { agent = normalizedAgent, cancellationToken });

        // Workspace summaries omit per-actor unread and archived counts.
        return await BuildThreadSummariesAsync(connection, rollups, unreadActor: null, preserveOrder: false, cancellationToken);
    }

    public async Task<IReadOnlyList<MailThreadSummary>> QueryParticipationThreadsAsync(
        string agent,
        int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedAgent = MailAgentName.Normalize(agent);

        await using var connection = await ConnectAsync(cancellationToken);

        // Ranks threads by the agent's own newest sent-or-received message in the
        // thread, not the thread's overall last message, so a later reply from
        // someone else does not outrank the agent's own activity.
        var rankSql =
            """
            SELECT thread_id
            FROM (
                SELECT thread_id, created_at FROM messages WHERE sender = @agent
                UNION
                SELECT m.thread_id, m.created_at
                FROM messages m
                JOIN message_recipients mr ON mr.message_id = m.id
                WHERE mr.recipient = @agent
            )
            GROUP BY thread_id
            ORDER BY MAX(created_at) DESC, thread_id DESC
            """;

        var rankParameters = new Dictionary<string, object?> { ["agent"] = normalizedAgent };

        if (limit is { } value)
        {
            rankParameters["limit"] = value;
            rankSql += " LIMIT @limit";
        }

        var rankedThreadIds = await ExecuteIdQueryAsync(connection, rankSql, rankParameters, cancellationToken);

        if (rankedThreadIds.Count == 0)
        {
            return [];
        }

        // Reads one rollup per ranked thread id, in ranking order, rather than a
        // single IN-list query: the thread's true message count and last-message
        // time (any sender), unlike the ranking key above.
        var rollups = new List<ThreadRollupRow>(rankedThreadIds.Count);

        foreach (var threadId in rankedThreadIds)
        {
            rollups.Add(await connection.QueryFirstAsync<ThreadRollupRow>(
                """
                SELECT
                    thread_id AS ThreadId,
                    COUNT(*) AS MessageCount,
                    MAX(created_at) AS LastMessageAt
                FROM messages
                WHERE thread_id = @threadId
                GROUP BY thread_id
                """,
                new { threadId, cancellationToken }));
        }

        return await BuildThreadSummariesAsync(connection, rollups, normalizedAgent, preserveOrder: true, cancellationToken);
    }

    /// <summary>
    /// Returns thread summaries ordered by last-message time and thread id, newest first,
    /// or in <paramref name="rollups"/> order when <paramref name="preserveOrder"/> is true.
    /// Unread and archived counts are null when <paramref name="unreadActor"/> is null.
    /// </summary>
    private static async Task<IReadOnlyList<MailThreadSummary>> BuildThreadSummariesAsync(
        SqliteConnection connection,
        IEnumerable<ThreadRollupRow> rollups,
        string? unreadActor,
        bool preserveOrder,
        CancellationToken cancellationToken)
    {
        var summaries = new List<MailThreadSummary>();

        foreach (var rollup in rollups)
        {
            var root = await connection.QueryFirstOrDefaultAsync<MailMessageRow>(
                $"SELECT {MailMessage.Columns} FROM messages WHERE id = @id",
                new { id = rollup.ThreadId, cancellationToken });

            var lastMessage = await connection.QueryFirstOrDefaultAsync<MailMessageRow>(
                $"""
                SELECT {MailMessage.Columns} FROM messages
                WHERE thread_id = @threadId
                ORDER BY created_at DESC, id DESC
                LIMIT 1
                """,
                new { threadId = rollup.ThreadId, cancellationToken });

            IReadOnlyList<string> lastRecipients = lastMessage is null
                ? []
                : (await GetRecipientsAsync(connection, lastMessage.Id, cancellationToken))
                    .Select(r => r.Name)
                    .ToArray();

            int? unreadCount = null;
            int? archivedCount = null;

            if (unreadActor is not null)
            {
                unreadCount = await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM message_recipients mr
                    JOIN messages m ON m.id = mr.message_id
                    WHERE m.thread_id = @threadId
                        AND mr.recipient = @actor
                        AND mr.read_at IS NULL
                    """,
                    new { threadId = rollup.ThreadId, actor = unreadActor, cancellationToken });

                archivedCount = await connection.ExecuteScalarAsync<int>(
                    """
                    SELECT COUNT(*)
                    FROM message_recipients mr
                    JOIN messages m ON m.id = mr.message_id
                    WHERE m.thread_id = @threadId
                        AND mr.recipient = @actor
                        AND mr.archived_at IS NOT NULL
                    """,
                    new { threadId = rollup.ThreadId, actor = unreadActor, cancellationToken });
            }

            summaries.Add(new MailThreadSummary
            {
                ThreadId = rollup.ThreadId,
                Subject = root?.Subject ?? "",
                MessageCount = rollup.MessageCount,
                LastMessageAt = DateTimeOffset.Parse(rollup.LastMessageAt, CultureInfo.InvariantCulture),
                LastSender = lastMessage?.Sender ?? "",
                LastRecipients = lastRecipients,
                BodyPreview = CreateBodyPreview(lastMessage?.Body ?? ""),
                UnreadCount = unreadCount,
                ArchivedCount = archivedCount
            });
        }

        if (preserveOrder)
        {
            return summaries;
        }

        return summaries
            .OrderByDescending(s => s.LastMessageAt)
            .ThenByDescending(s => s.ThreadId, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Collapses whitespace and trims the body, retaining at most
    /// <see cref="MailThreadSummary.BodyPreviewMaxLength"/> characters before
    /// appending an ellipsis when truncated.
    /// </summary>
    private static string CreateBodyPreview(string body)
    {
        var collapsed = string.Join(' ', body.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        return collapsed.Length <= MailThreadSummary.BodyPreviewMaxLength
            ? collapsed
            : collapsed[..MailThreadSummary.BodyPreviewMaxLength].TrimEnd() + "…";
    }

    public async Task<IReadOnlyList<MailMessage>> SearchAsync(
        string actor,
        string text,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);
        var escapedText = EscapeLikeText(text);

        await using var connection = await ConnectAsync(cancellationToken);

        var rows = await connection.QueryAsync<MailMessageRow>(
            $"""
            SELECT DISTINCT {MailMessage.Columns} FROM messages m
            WHERE (
                m.sender = @actor
                OR EXISTS (
                    SELECT 1 FROM message_recipients mr
                    WHERE mr.message_id = m.id AND mr.recipient = @actor
                )
            )
            AND (
                LOWER(m.subject) LIKE '%' || LOWER(@text) || '%' ESCAPE '\'
                OR LOWER(m.body) LIKE '%' || LOWER(@text) || '%' ESCAPE '\'
                OR LOWER(m.sender) LIKE '%' || LOWER(@text) || '%' ESCAPE '\'
            )
            ORDER BY m.created_at DESC, m.id DESC
            """,
            new { actor = normalizedActor, text = escapedText, cancellationToken });

        var messages = new List<MailMessage>();

        foreach (var row in rows)
        {
            var recipients = await GetRecipientsAsync(connection, row.Id, cancellationToken);
            messages.Add(row.ToMailMessage(recipients));
        }

        return messages;
    }

    public async Task<int> CountUnreadAsync(
        string actor,
        CancellationToken cancellationToken)
    {
        var normalizedActor = MailAgentName.Normalize(actor);

        await using var connection = await ConnectAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(
            """
            SELECT COUNT(*) FROM message_recipients
            WHERE recipient = @actor AND read_at IS NULL AND archived_at IS NULL
            """,
            new { actor = normalizedActor, cancellationToken });
    }

    public async Task<IReadOnlyList<MailMessage>> QuerySentAsync(
        string sender,
        int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedSender = MailAgentName.Normalize(sender);

        var sql =
            """
            SELECT id FROM messages
            WHERE sender = @sender
            ORDER BY created_at DESC, id DESC
            """;

        var parameters = new Dictionary<string, object?> { ["sender"] = normalizedSender };

        if (limit is { } value)
        {
            parameters["limit"] = value;
            sql += " LIMIT @limit";
        }

        await using var connection = await ConnectAsync(cancellationToken);

        var ids = await ExecuteIdQueryAsync(connection, sql, parameters, cancellationToken);
        var messages = new List<MailMessage>(ids.Count);

        foreach (var id in ids)
        {
            var message = await GetMessageAsync(connection, id, cancellationToken);

            if (message is not null)
            {
                messages.Add(message);
            }
        }

        return messages;
    }

    /// <summary>
    /// Normalizes and dedupes recipient names: to and cc are disjoint with
    /// to winning, repeated names collapse to their first occurrence, and
    /// the ordinal reflects that first-occurrence order.
    /// </summary>
    private static List<MailRecipient> BuildRecipients(
        IReadOnlyList<string> to,
        IReadOnlyList<string> cc)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var recipients = new List<MailRecipient>();

        foreach (var raw in to)
        {
            var name = MailAgentName.Normalize(raw);

            if (seen.Add(name))
            {
                recipients.Add(new MailRecipient
                {
                    Name = name,
                    Kind = MailRecipientKinds.To,
                    Ordinal = recipients.Count
                });
            }
        }

        foreach (var raw in cc)
        {
            var name = MailAgentName.Normalize(raw);

            if (seen.Add(name))
            {
                recipients.Add(new MailRecipient
                {
                    Name = name,
                    Kind = MailRecipientKinds.Cc,
                    Ordinal = recipients.Count
                });
            }
        }

        return recipients;
    }

    /// <summary>
    /// Ensures every recipient has an agent row, implicit-creating one for
    /// any name that has never registered or acted, and returns the names,
    /// in recipient order, whose row is implicit, whether it already was or
    /// was just created here.
    /// </summary>
    private async Task<List<string>> EnsureRecipientsAsync(
        IReadOnlyList<MailRecipient> recipients,
        CancellationToken cancellationToken)
    {
        var unregistered = new List<string>();

        foreach (var recipient in recipients)
        {
            var agent = await agentRegistry.EnsureImplicitAsync(recipient.Name, cancellationToken);

            if (agent.Implicit)
            {
                unregistered.Add(recipient.Name);
            }
        }

        return unregistered;
    }

    private static async Task InsertRecipientsAsync(
        SqliteConnection connection,
        string messageId,
        IReadOnlyList<MailRecipient> recipients,
        CancellationToken cancellationToken,
        DbTransaction transaction)
    {
        foreach (var recipient in recipients)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO message_recipients (
                    message_id,
                    recipient,
                    kind,
                    ordinal
                )
                VALUES (
                    @MessageId,
                    @Recipient,
                    @Kind,
                    @Ordinal
                )
                """,
                new
                {
                    MessageId = messageId,
                    Recipient = recipient.Name,
                    Kind = recipient.Kind,
                    Ordinal = recipient.Ordinal,
                    cancellationToken
                },
                transaction);
        }
    }

    private async Task<string> CreateMessageIdAsync(
        SqliteConnection connection,
        string seed,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null)
    {
        var messageCount = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM messages",
            transaction: transaction);

        for (var attempt = 0; attempt < MaxIdAttempts; attempt++)
        {
            var id = IdPrefix + CreateIdSuffix(seed, messageCount, attempt);

            var exists = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM messages WHERE id = @id",
                new { id, cancellationToken },
                transaction);

            if (exists == 0)
            {
                return id;
            }
        }

        throw new ExitException("Could not allocate a unique message ID.");
    }

    private static string CreateIdSuffix(string seed, long messageCount, int attempt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{messageCount}|{attempt}"));
        var length = MinIdLength + attempt / 3;
        var suffix = new char[length];

        for (var i = 0; i < length; i++)
        {
            suffix[i] = IdAlphabet[hash[i] % IdAlphabet.Length];
        }

        return new string(suffix);
    }

    /// <summary>
    /// Escapes percent signs, underscores, and backslashes for literal matching in a LIKE pattern.
    /// </summary>
    private static string EscapeLikeText(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    internal sealed class MailMessageRow
    {
        public required string Id { get; init; }
        public required string ThreadId { get; init; }
        public string? InReplyTo { get; init; }
        public required string Sender { get; init; }
        public required string Subject { get; init; }
        public required string Body { get; init; }
        public required string CreatedAt { get; init; }

        public MailMessage ToMailMessage(IReadOnlyList<MailRecipient> recipients) => new()
        {
            Id = Id,
            ThreadId = ThreadId,
            InReplyTo = InReplyTo,
            Sender = Sender,
            Subject = Subject,
            Body = Body,
            CreatedAt = DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture),
            Recipients = recipients
        };
    }

    internal sealed class MailRecipientRow
    {
        public required string Name { get; init; }
        public required string Kind { get; init; }
        public required int Ordinal { get; init; }
        public string? ReadAt { get; init; }
        public string? ArchivedAt { get; init; }

        public MailRecipient ToMailRecipient() => new()
        {
            Name = Name,
            Kind = Kind,
            Ordinal = Ordinal,
            ReadAt = ReadAt is null ? null : DateTimeOffset.Parse(ReadAt, CultureInfo.InvariantCulture),
            ArchivedAt = ArchivedAt is null
                ? null
                : DateTimeOffset.Parse(ArchivedAt, CultureInfo.InvariantCulture)
        };
    }

    /// <summary>
    /// A thread id, message count, and last-message timestamp.
    /// </summary>
    internal sealed class ThreadRollupRow
    {
        public required string ThreadId { get; init; }
        public required int MessageCount { get; init; }
        public required string LastMessageAt { get; init; }
    }
}

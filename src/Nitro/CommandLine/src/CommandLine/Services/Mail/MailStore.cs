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
    IAgentRegistry agentRegistry) : IMailStore
{
    private const string IdPrefix = "m-";
    private const string IdAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int MinIdLength = 6;
    private const int MaxIdAttempts = 10;

    /// <summary>
    /// Opens a connection to a new or existing workspace database, applies
    /// the schema, and stamps the current schema version. Returns the open
    /// connection so seeding helpers in tests can write against it directly.
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

        // Auto-registers the sender through the shared registry before the
        // message write transaction opens; the registry manages its own
        // connection, so this upsert is not part of that transaction.
        await agentRegistry.TouchAsync(sender, cancellationToken);

        // Ensures every recipient has an agent row, implicit-created when
        // they have never registered or acted, before the write transaction
        // opens: the registry manages its own connection, and an open
        // transaction on the write connection would block it from starting
        // one of its own against the same file, mirroring the sender touch
        // above.
        var unregistered = await EnsureRecipientsAsync(recipients, cancellationToken);

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
            Unregistered = unregistered
        };
    }

    public async Task<MailMessage> ReplyMessageAsync(
        string inReplyToId,
        string sender,
        string body,
        CancellationToken cancellationToken)
    {
        var actor = MailAgentName.Normalize(sender);
        var now = timeProvider.GetUtcNow();

        var (original, root, recipients) =
            await ResolveReplyAsync(inReplyToId, actor, cancellationToken);

        // Auto-registers the replying actor through the shared registry, now
        // that eligibility is confirmed, before opening the write
        // connection below; the registry manages its own connection, and an
        // open transaction on another connection to the same file blocks it
        // from starting one of its own.
        await agentRegistry.TouchAsync(actor, cancellationToken);

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
            Recipients = recipients
        };
    }

    /// <summary>
    /// Reads the original message and the thread root, validates the actor
    /// is authorized to reply, and computes the reply's recipient set, all
    /// against a short-lived read connection closed before this method
    /// returns. Throws <see cref="ExitException"/> when the original
    /// message does not exist, the actor is not a participant, or the
    /// computed recipient set is empty.
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

    // The WHERE/LIMIT clauses here are assembled at runtime from the filter,
    // so the SQL text is never a call-site literal; Dapper.AOT can only
    // intercept calls whose SQL it can read at compile time. This is
    // executed through plain ADO.NET rather than Dapper's reflection
    // fallback, mirroring TaskStore.BuildTaskFilterClause.
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

    // Every id is validated as addressed to the actor before any write
    // happens, giving the bulk mutations their all-or-nothing behavior,
    // mirroring TaskStore.CloseTaskAsync.
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

        var summaries = new List<MailThreadSummary>();

        foreach (var rollup in rollups)
        {
            var root = await connection.QueryFirstOrDefaultAsync<MailMessageRow>(
                $"SELECT {MailMessage.Columns} FROM messages WHERE id = @id",
                new { id = rollup.ThreadId, cancellationToken });

            var lastSender = await connection.ExecuteScalarAsync<string?>(
                """
                SELECT sender FROM messages
                WHERE thread_id = @threadId
                ORDER BY created_at DESC, id DESC
                LIMIT 1
                """,
                new { threadId = rollup.ThreadId, cancellationToken });

            var unreadCount = await connection.ExecuteScalarAsync<int>(
                """
                SELECT COUNT(*)
                FROM message_recipients mr
                JOIN messages m ON m.id = mr.message_id
                WHERE m.thread_id = @threadId
                    AND mr.recipient = @actor
                    AND mr.read_at IS NULL
                """,
                new { threadId = rollup.ThreadId, actor = normalizedActor, cancellationToken });

            summaries.Add(new MailThreadSummary
            {
                ThreadId = rollup.ThreadId,
                Subject = root?.Subject ?? "",
                MessageCount = rollup.MessageCount,
                LastMessageAt = DateTimeOffset.Parse(rollup.LastMessageAt, CultureInfo.InvariantCulture),
                LastSender = lastSender ?? "",
                UnreadCount = unreadCount
            });
        }

        return summaries
            .OrderByDescending(s => s.LastMessageAt)
            .ThenByDescending(s => s.ThreadId, StringComparer.Ordinal)
            .ToList();
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

        // The LIMIT clause is assembled at runtime from the optional limit,
        // so the SQL text is never a call-site literal; executed through
        // plain ADO.NET rather than Dapper's reflection fallback, mirroring
        // BuildInboxQuery/ExecuteIdQueryAsync above.
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
    /// Escapes the LIKE wildcard characters '%' and '_' (and the escape
    /// character itself) so search text is matched literally, other than the
    /// wildcards this store wraps around it.
    /// </summary>
    private static string EscapeLikeText(string value)
        => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    // These nested row types are internal, not private: Dapper.AOT's
    // generated interceptors live outside MailStore and cannot reference a
    // private nested type, so a private row type would silently fall back to
    // Dapper's reflection-emit deserializer.
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
    /// A thread's message count and last-message timestamp, for
    /// <see cref="QueryThreadsAsync"/>; the subject and last sender are
    /// filled in by follow-up queries per thread.
    /// </summary>
    internal sealed class ThreadRollupRow
    {
        public required string ThreadId { get; init; }
        public required int MessageCount { get; init; }
        public required string LastMessageAt { get; init; }
    }
}

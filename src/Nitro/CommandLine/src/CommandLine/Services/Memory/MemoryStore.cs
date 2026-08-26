using System.Data.Common;
using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Workspace;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Curated memories and the journal, in the workspace database beside tasks
/// and mail: the curated vertical (save, update, forget, show, recent,
/// search) and the journal vertical (log, promote). Search runs against the
/// <c>memory_curated_fts</c> table the schema's own triggers maintain, so
/// there is no index to rebuild or fall out of step.
/// </summary>
internal sealed class MemoryStore(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database) : IMemoryStore
{
    public string? FindWorkspaceDirectory()
        => AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    public async Task<MemoryRecord> SaveAsync(
        MemoryRecordCreation creation, CancellationToken cancellationToken)
    {
        var type = ValidateType(creation.Type);
        var tags = NormalizeTags(creation.Tags);
        var actor = ValidateActor(creation.Actor);
        var now = timeProvider.GetUtcNow();
        var id = MemoryId.New(timeProvider);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO memory_curated (id, type, body, created_at, updated_at, created_by)
            VALUES (@id, @type, @body, @now, @now, @actor);
            """,
            new { id, type, body = creation.Text, now, actor },
            transaction);

        await InsertTagsAsync(connection, transaction, id, tags);
        await transaction.CommitAsync(cancellationToken);

        return new MemoryRecord
        {
            Id = id,
            Type = type,
            Tags = tags,
            Body = creation.Text,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = actor
        };
    }

    public async Task<MemoryRecord> UpdateAsync(
        string id, MemoryRecordUpdate update, CancellationToken cancellationToken)
    {
        var type = update.TypeGiven ? ValidateType(update.Type ?? "") : null;
        var addTags = NormalizeTags(update.AddTags);
        var removeTags = NormalizeTags(update.RemoveTags);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        _ = await RequireCuratedAsync(connection, transaction, id, cancellationToken);
        var now = timeProvider.GetUtcNow();

        await connection.ExecuteAsync(
            """
            UPDATE memory_curated SET
                type = COALESCE(@type, type),
                body = COALESCE(@body, body),
                updated_at = @now
            WHERE id = @id;
            """,
            new { id, type, body = update.TextGiven ? update.Text : null, now },
            transaction);

        foreach (var tag in removeTags)
        {
            await connection.ExecuteAsync(
                "DELETE FROM memory_curated_tags WHERE id = @id AND tag = @tag;",
                new { id, tag },
                transaction);
        }

        await InsertTagsAsync(connection, transaction, id, addTags);

        var updated = await RequireCuratedAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return updated;
    }

    public async Task<MemoryRecord> ForgetAsync(string id, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var record = await RequireCuratedAsync(connection, transaction, id, cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM memory_curated WHERE id = @id;",
            new { id },
            transaction);
        await transaction.CommitAsync(cancellationToken);

        return record;
    }

    public async Task<MemoryRecord?> FindAsync(string id, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await FindCuratedAsync(connection, transaction: null, id, cancellationToken);
    }

    public async Task<MemoryRecord> GetRequiredAsync(string id, CancellationToken cancellationToken)
        => await FindAsync(id, cancellationToken) ?? throw NotFound(id);

    public async Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(
        int? limit, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var ids = await connection.QueryAsync<string>(
            """
            SELECT id FROM memory_curated
            ORDER BY updated_at DESC, id
            LIMIT @limit;
            """,
            new { limit = limit ?? -1 });

        return await LoadCuratedAsync(connection, ids.ToList());
    }

    public async Task<IReadOnlyList<MemoryRecord>> SearchCuratedAsync(
        string query,
        IReadOnlyList<string> tags,
        string? type,
        DateTimeOffset? since,
        int? limit,
        CancellationToken cancellationToken)
    {
        var normalizedTags = NormalizeTags(tags);
        var normalizedType = type is null ? null : ValidateType(type);

        await using var connection = await ConnectAsync(cancellationToken);

        // The query is quoted into a single FTS5 phrase rather than passed
        // through: a caller's text is search input, never query syntax.
        var match = MemoryFtsQuery.BuildLiteralMatch(query);

        var sql =
            """
            SELECT c.id
            FROM memory_curated_fts f
            JOIN memory_curated c ON c.id = f.id
            WHERE memory_curated_fts MATCH @match
            """;

        if (normalizedType is not null)
        {
            sql += "\n  AND c.type = @type";
        }

        if (since is { } minimum)
        {
            _ = minimum;
            sql += "\n  AND c.updated_at >= @since";
        }

        foreach (var (tag, index) in normalizedTags.Select((tag, index) => (tag, index)))
        {
            _ = tag;
            sql += "\n  AND EXISTS (SELECT 1 FROM memory_curated_tags t "
                + $"WHERE t.id = c.id AND t.tag = @tag{index})";
        }

        sql += "\nORDER BY f.rank, c.updated_at DESC, c.id\nLIMIT @limit;";

        var parameters = new DynamicParameters();
        parameters.Add("match", match);
        parameters.Add("type", normalizedType);
        parameters.Add("since", since);
        parameters.Add("limit", limit ?? -1);

        for (var index = 0; index < normalizedTags.Count; index++)
        {
            parameters.Add($"tag{index}", normalizedTags[index]);
        }

        var ids = await connection.QueryAsync<string>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

        return await LoadCuratedAsync(connection, ids.ToList());
    }

    public async Task<MemoryJournalEntry> LogAsync(
        MemoryJournalEntryCreation creation, CancellationToken cancellationToken)
    {
        var actor = ValidateActor(creation.Actor);
        var now = timeProvider.GetUtcNow();
        var id = MemoryId.New(timeProvider);

        await using var connection = await ConnectAsync(cancellationToken);

        await connection.ExecuteAsync(
            """
            INSERT INTO memory_journal (id, body, created_at, created_by)
            VALUES (@id, @body, @now, @actor);
            """,
            new { id, body = creation.Text, now, actor });

        return new MemoryJournalEntry
        {
            Id = id,
            Body = creation.Text,
            CreatedAt = now,
            CreatedBy = actor
        };
    }

    public async Task<MemoryJournalEntry?> FindJournalEntryAsync(string id, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<JournalRow>(
            "SELECT id AS Id, body AS Body, created_at AS CreatedAt, created_by AS CreatedBy "
            + "FROM memory_journal WHERE id = @id;",
            new { id });

        return row?.ToEntry();
    }

    public async Task<MemoryJournalEntry> GetRequiredJournalEntryAsync(
        string id, CancellationToken cancellationToken)
        => await FindJournalEntryAsync(id, cancellationToken)
            ?? throw new ExitException($"Journal entry '{id}' does not exist.");

    public async Task<IReadOnlyList<MemoryJournalEntry>> GetRecentJournalAsync(
        int? limit, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var entries = await connection.QueryAsync<JournalRow>(
            "SELECT id AS Id, body AS Body, created_at AS CreatedAt, created_by AS CreatedBy "
            + "FROM memory_journal ORDER BY created_at DESC, id LIMIT @limit;",
            new { limit = limit ?? -1 });

        return entries.Select(row => row.ToEntry()).ToList();
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> SearchJournalAsync(
        string query, DateTimeOffset? since, int? limit, CancellationToken cancellationToken)
    {
        var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await using var connection = await ConnectAsync(cancellationToken);

        var entries = await connection.QueryAsync<JournalRow>(
            "SELECT id AS Id, body AS Body, created_at AS CreatedAt, created_by AS CreatedBy "
            + "FROM memory_journal WHERE (@since IS NULL OR created_at >= @since) "
            + "ORDER BY created_at DESC, id;",
            new { since });

        // Matched in memory rather than in SQL: the journal has no FTS
        // index, and the match is every word as a literal substring, which
        // LIKE cannot express without escaping the caller's text itself.
        var matched = entries.Where(row => MatchesAllWords(row.Body, words));

        return (limit is { } max ? matched.Take(max) : matched).Select(row => row.ToEntry()).ToList();
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> GetUnpromotedJournalEntriesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        var entries = await connection.QueryAsync<JournalRow>(
            """
            SELECT j.id AS Id, j.body AS Body, j.created_at AS CreatedAt, j.created_by AS CreatedBy
            FROM memory_journal j
            WHERE NOT EXISTS (SELECT 1 FROM memory_curated c WHERE c.promoted_from = j.id)
            ORDER BY j.created_at DESC, j.id;
            """);

        return entries.Select(row => row.ToEntry()).ToList();
    }

    public async Task<MemoryPromotionOutcome> PromoteAsync(
        string journalId,
        string type,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken)
    {
        var normalizedType = ValidateType(type);
        var normalizedTags = NormalizeTags(tags);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var entry = await connection.QueryFirstOrDefaultAsync<JournalRow>(
            new CommandDefinition(
                "SELECT id AS Id, body AS Body, created_at AS CreatedAt, created_by AS CreatedBy "
                + "FROM memory_journal WHERE id = @journalId;",
                new { journalId },
                transaction,
                cancellationToken: cancellationToken))
            ?? throw new ExitException($"Journal entry '{journalId}' does not exist.");

        var id = MemoryPromotedId.Derive(journalId);
        var now = timeProvider.GetUtcNow();

        // INSERT OR IGNORE against the unique promoted_from index: a second
        // promote of the same entry, including a concurrent one, affects no
        // rows and reports the memory the first one produced.
        var inserted = await connection.ExecuteAsync(
            """
            INSERT OR IGNORE INTO memory_curated (
                id, type, body, created_at, updated_at, created_by, promoted_from
            ) VALUES (@id, @type, @body, @now, @now, @actor, @journalId);
            """,
            new { id, type = normalizedType, body = entry.Body, now, actor = entry.CreatedBy, journalId },
            transaction);

        if (inserted == 0)
        {
            var existing = await FindPromotedAsync(connection, transaction, journalId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new MemoryPromotionOutcome(existing, AlreadyPromoted: true);
        }

        await InsertTagsAsync(connection, transaction, id, normalizedTags);
        var record = await RequireCuratedAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new MemoryPromotionOutcome(record, AlreadyPromoted: false);
    }

    private static async Task InsertTagsAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string id,
        IReadOnlyList<string> tags)
    {
        foreach (var tag in tags)
        {
            await connection.ExecuteAsync(
                "INSERT OR IGNORE INTO memory_curated_tags (id, tag) VALUES (@id, @tag);",
                new { id, tag },
                transaction);
        }
    }

    /// <summary>
    /// Loads full records for the given ids, preserving the order the ids
    /// were given in: the ordering is decided by the query that produced
    /// them (recency, or FTS rank), which a second lookup must not disturb.
    /// </summary>
    private static async Task<IReadOnlyList<MemoryRecord>> LoadCuratedAsync(
        SqliteConnection connection,
        IReadOnlyList<string> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        var idList = string.Join(", ", ids.Select(id => $"'{MemoryId.Require(id)}'"));

        var rows = await connection.QueryAsync<CuratedRow>(
            "SELECT id AS Id, type AS Type, body AS Body, created_at AS CreatedAt, "
            + "updated_at AS UpdatedAt, created_by AS CreatedBy, promoted_from AS PromotedFrom "
            + $"FROM memory_curated WHERE id IN ({idList});");

        var tagRows = await connection.QueryAsync<CuratedTagRow>(
            $"SELECT id AS Id, tag AS Tag FROM memory_curated_tags WHERE id IN ({idList}) ORDER BY tag;");

        var tagsById = tagRows
            .GroupBy(row => row.Id)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(r => r.Tag).ToArray());

        var byId = rows.ToDictionary(row => row.Id, row => row.ToRecord(tagsById));

        return ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
    }

    private static async Task<MemoryRecord?> FindCuratedAsync(
        SqliteConnection connection,
        DbTransaction? transaction,
        string id,
        CancellationToken cancellationToken)
    {
        var row = await connection.QueryFirstOrDefaultAsync<CuratedRow>(
            new CommandDefinition(
                "SELECT id AS Id, type AS Type, body AS Body, created_at AS CreatedAt, "
                + "updated_at AS UpdatedAt, created_by AS CreatedBy, promoted_from AS PromotedFrom "
                + "FROM memory_curated WHERE id = @id;",
                new { id },
                transaction,
                cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var tags = await connection.QueryAsync<string>(
            new CommandDefinition(
                "SELECT tag FROM memory_curated_tags WHERE id = @id ORDER BY tag;",
                new { id },
                transaction,
                cancellationToken: cancellationToken));

        return row.ToRecord(tags.ToArray());
    }

    private static async Task<MemoryRecord> FindPromotedAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string journalId,
        CancellationToken cancellationToken)
    {
        var id = await connection.QueryFirstAsync<string>(
            new CommandDefinition(
                "SELECT id FROM memory_curated WHERE promoted_from = @journalId;",
                new { journalId },
                transaction,
                cancellationToken: cancellationToken));

        return (await FindCuratedAsync(connection, transaction, id, cancellationToken))!;
    }

    private static async Task<MemoryRecord> RequireCuratedAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        string id,
        CancellationToken cancellationToken)
        => await FindCuratedAsync(connection, transaction, id, cancellationToken) ?? throw NotFound(id);

    private static ExitException NotFound(string id) => new($"Memory '{id}' does not exist.");

    private static bool MatchesAllWords(string body, IReadOnlyList<string> words)
    {
        foreach (var word in words)
        {
            if (!body.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string ValidateType(string type)
    {
        var normalized = MemoryTypes.Normalize(type);

        if (!MemoryTypes.IsValid(normalized))
        {
            throw new ExitException(
                $"The type '{type}' is invalid. A type may contain only lowercase letters, digits, "
                + "and hyphens, up to 40 characters.");
        }

        return normalized;
    }

    private static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var normalized = new List<string>();

        foreach (var tag in tags)
        {
            var value = ValidateTag(tag);

            if (seen.Add(value))
            {
                normalized.Add(value);
            }
        }

        return normalized;
    }

    private static string ValidateTag(string tag)
    {
        var normalized = MemoryTags.Normalize(tag);

        if (!MemoryTags.IsValid(normalized))
        {
            throw new ExitException(
                $"The tag '{tag}' is invalid. A tag may contain only lowercase letters, digits, "
                + "and hyphens, up to 40 characters.");
        }

        return normalized;
    }

    private static string ValidateActor(string actor)
    {
        var trimmed = actor.Trim();

        if (trimmed.Length == 0 || trimmed.Contains('\n') || trimmed.Contains('\r'))
        {
            throw new ExitException("The actor must not be empty or contain line breaks.");
        }

        return trimmed;
    }

    private sealed class JournalRow
    {
        public required string Id { get; init; }
        public required string Body { get; init; }
        public required string CreatedAt { get; init; }
        public required string CreatedBy { get; init; }

        public MemoryJournalEntry ToEntry() => new()
        {
            Id = Id,
            Body = Body,
            CreatedAt = DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture),
            CreatedBy = CreatedBy
        };
    }

    private sealed class CuratedTagRow
    {
        public required string Id { get; init; }
        public required string Tag { get; init; }
    }

    private sealed class CuratedRow
    {
        public required string Id { get; init; }
        public required string Type { get; init; }
        public required string Body { get; init; }
        public required string CreatedAt { get; init; }
        public required string UpdatedAt { get; init; }
        public required string CreatedBy { get; init; }
        public string? PromotedFrom { get; init; }

        public MemoryRecord ToRecord(IReadOnlyDictionary<string, IReadOnlyList<string>> tagsById)
            => ToRecord(tagsById.TryGetValue(Id, out var tags) ? tags : []);

        public MemoryRecord ToRecord(IReadOnlyList<string> tags) => new()
        {
            Id = Id,
            Type = Type,
            Tags = tags,
            Body = Body,
            CreatedAt = DateTimeOffset.Parse(CreatedAt, CultureInfo.InvariantCulture),
            UpdatedAt = DateTimeOffset.Parse(UpdatedAt, CultureInfo.InvariantCulture),
            CreatedBy = CreatedBy,
            PromotedFrom = PromotedFrom
        };
    }
}

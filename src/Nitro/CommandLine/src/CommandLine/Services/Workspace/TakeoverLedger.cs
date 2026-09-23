using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class TakeoverLedger(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database) : ITakeoverLedger
{
    private const string IdPrefix = "to-";
    private const string IdAlphabet = "0123456789abcdefghijklmnopqrstuvwxyz";
    private const int MinIdLength = 6;
    private const int MaxIdAttempts = 10;

    public async Task<TakeoverRecord> RecordAsync(
        TakeoverRecordCreation creation,
        IReadOnlyList<TakeoverItem> items,
        CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
        var createdAt = timeProvider.GetUtcNow();

        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var id = await CreateTakeoverIdAsync(
            connection,
            $"{creation.FromActor}|{creation.ToActor}|{creation.Actor}|{createdAt:O}",
            cancellationToken,
            transaction);

        await connection.ExecuteAsync(
            new CommandDefinition(
                """
            INSERT INTO agent_takeovers (
                id, from_actor, to_actor, actor, created_at, forced, role, reason
            )
            VALUES (
                @Id, @FromActor, @ToActor, @Actor, @CreatedAt, @Forced, @Role, @Reason
            );
            """,
                new
                {
                    Id = id,
                    creation.FromActor,
                    creation.ToActor,
                    creation.Actor,
                    CreatedAt = createdAt,
                    creation.Forced,
                    creation.Role,
                    creation.Reason
                },
                transaction,
                cancellationToken: cancellationToken));

        foreach (var item in items)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    """
                INSERT INTO agent_takeover_items (takeover_id, kind, item_id)
                VALUES (@TakeoverId, @Kind, @ItemId);
                """,
                    new { TakeoverId = id, item.Kind, item.ItemId },
                    transaction,
                    cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);

        return new TakeoverRecord
        {
            Id = id,
            FromActor = creation.FromActor,
            ToActor = creation.ToActor,
            Actor = creation.Actor,
            CreatedAt = createdAt,
            Forced = creation.Forced,
            Role = creation.Role,
            Reason = creation.Reason,
            Items = items.ToArray()
        };
    }

    public async Task<IReadOnlyList<TakeoverRecord>> QueryAsync(
        TakeoverFilter filter,
        CancellationToken cancellationToken)
    {
        var workspaceDirectory = FindWorkspaceDirectory()
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        await using var connection = await database.ConnectAsync(workspaceDirectory, cancellationToken);

        var where = new List<string>();
        var parameters = new Dictionary<string, object?>();

        if (filter.Actor is not null)
        {
            where.Add("(t.from_actor = @Actor OR t.to_actor = @Actor)");
            parameters["Actor"] = filter.Actor;
        }

        if (filter.MessageId is not null)
        {
            // message_sender items only exist on takeovers recorded before senders
            // stopped moving; they stay in this filter so old ledger rows still match.
            where.Add(
                """
                EXISTS (
                    SELECT 1
                    FROM agent_takeover_items AS i
                    WHERE i.takeover_id = t.id
                        AND i.item_id = @MessageId
                        AND i.kind IN ('message_sender', 'message_recipient')
                )
                """);
            parameters["MessageId"] = filter.MessageId;
        }

        if (filter.TaskId is not null)
        {
            where.Add(
                """
                EXISTS (
                    SELECT 1
                    FROM agent_takeover_items AS i
                    WHERE i.takeover_id = t.id
                        AND i.kind = 'task'
                        AND i.item_id = @TaskId
                )
                """);
            parameters["TaskId"] = filter.TaskId;
        }

        parameters["Limit"] = filter.Limit;

        var sql =
            $"""
            SELECT {TakeoverRecord.Columns}
            FROM agent_takeovers AS t
            {(where.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", where)}")}
            ORDER BY t.created_at DESC, t.id DESC
            LIMIT COALESCE(@Limit, -1);
            """;

        var rows = await ExecuteRecordQueryAsync(connection, sql, parameters, cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var itemParameters = new Dictionary<string, object?>();
        var itemNames = new string[rows.Count];

        for (var index = 0; index < rows.Count; index++)
        {
            var name = $"TakeoverId{index}";
            itemNames[index] = $"@{name}";
            itemParameters[name] = rows[index].Id;
        }

        var itemsSql =
            $"""
            SELECT takeover_id AS TakeoverId, kind AS Kind, item_id AS ItemId
            FROM agent_takeover_items
            WHERE takeover_id IN ({string.Join(", ", itemNames)})
            ORDER BY takeover_id, kind, item_id;
            """;

        var itemRows = await ExecuteItemQueryAsync(connection, itemsSql, itemParameters, cancellationToken);
        var itemsByTakeoverId = itemRows
            .GroupBy(item => item.TakeoverId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TakeoverItem>)group
                .Select(item => new TakeoverItem { Kind = item.Kind, ItemId = item.ItemId })
                .ToArray(), StringComparer.Ordinal);

        return rows.Select(row => new TakeoverRecord
        {
            Id = row.Id,
            FromActor = row.FromActor,
            ToActor = row.ToActor,
            Actor = row.Actor,
            CreatedAt = DateTimeOffset.Parse(row.CreatedAt, CultureInfo.InvariantCulture),
            Forced = row.Forced,
            Role = row.Role,
            Reason = row.Reason,
            Items = itemsByTakeoverId.GetValueOrDefault(row.Id, [])
        }).ToArray();
    }

    private static async Task<List<TakeoverRecordRow>> ExecuteRecordQueryAsync(
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

        var results = new List<TakeoverRecordRow>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var id = reader.GetOrdinal("Id");
        var fromActor = reader.GetOrdinal("FromActor");
        var toActor = reader.GetOrdinal("ToActor");
        var actor = reader.GetOrdinal("Actor");
        var createdAt = reader.GetOrdinal("CreatedAt");
        var forced = reader.GetOrdinal("Forced");
        var role = reader.GetOrdinal("Role");
        var reason = reader.GetOrdinal("Reason");

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TakeoverRecordRow
            {
                Id = reader.GetString(id),
                FromActor = reader.GetString(fromActor),
                ToActor = reader.GetString(toActor),
                Actor = reader.GetString(actor),
                CreatedAt = reader.GetString(createdAt),
                Forced = reader.GetBoolean(forced),
                Role = reader.IsDBNull(role) ? null : reader.GetString(role),
                Reason = reader.IsDBNull(reason) ? null : reader.GetString(reason)
            });
        }

        return results;
    }

    private static async Task<List<TakeoverItemRow>> ExecuteItemQueryAsync(
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

        var results = new List<TakeoverItemRow>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var takeoverId = reader.GetOrdinal("TakeoverId");
        var kind = reader.GetOrdinal("Kind");
        var itemId = reader.GetOrdinal("ItemId");

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TakeoverItemRow
            {
                TakeoverId = reader.GetString(takeoverId),
                Kind = reader.GetString(kind),
                ItemId = reader.GetString(itemId)
            });
        }

        return results;
    }

    private string? FindWorkspaceDirectory()
        => AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory());

    private static async Task<string> CreateTakeoverIdAsync(
        SqliteConnection connection,
        string seed,
        CancellationToken cancellationToken,
        DbTransaction transaction)
    {
        var takeoverCount = await connection.ExecuteScalarAsync<long>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM agent_takeovers",
                transaction: transaction,
                cancellationToken: cancellationToken));

        for (var attempt = 0; attempt < MaxIdAttempts; attempt++)
        {
            var id = IdPrefix + CreateIdSuffix(seed, takeoverCount, attempt);
            var exists = await connection.ExecuteScalarAsync<long>(
                new CommandDefinition(
                    "SELECT COUNT(*) FROM agent_takeovers WHERE id = @Id",
                    new { Id = id },
                    transaction,
                    cancellationToken: cancellationToken));

            if (exists == 0)
            {
                return id;
            }
        }

        throw new ExitException("Could not allocate a unique takeover ID.");
    }

    private static string CreateIdSuffix(string seed, long takeoverCount, int attempt)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{seed}|{takeoverCount}|{attempt}"));
        var length = MinIdLength + attempt / 3;
        var suffix = new char[length];

        for (var index = 0; index < length; index++)
        {
            suffix[index] = IdAlphabet[hash[index] % IdAlphabet.Length];
        }

        return new string(suffix);
    }

    private sealed class TakeoverItemRow
    {
        public required string TakeoverId { get; init; }
        public required string Kind { get; init; }
        public required string ItemId { get; init; }
    }

    internal sealed class TakeoverRecordRow
    {
        public required string Id { get; init; }
        public required string FromActor { get; init; }
        public required string ToActor { get; init; }
        public required string Actor { get; init; }
        public required string CreatedAt { get; init; }
        public required bool Forced { get; init; }
        public string? Role { get; init; }
        public string? Reason { get; init; }
    }
}

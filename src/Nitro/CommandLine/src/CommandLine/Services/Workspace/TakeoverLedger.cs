using System.Data.Common;
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
                createdAt,
                creation.Forced,
                creation.Role,
                creation.Reason
            },
            transaction);

        foreach (var item in items)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO agent_takeover_items (takeover_id, kind, item_id)
                VALUES (@TakeoverId, @Kind, @ItemId);
                """,
                new { TakeoverId = id, item.Kind, item.ItemId },
                transaction);
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
        var parameters = new DynamicParameters();

        if (filter.Actor is not null)
        {
            where.Add("(t.from_actor = @Actor OR t.to_actor = @Actor)");
            parameters.Add("Actor", filter.Actor);
        }

        if (filter.MessageId is not null)
        {
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
            parameters.Add("MessageId", filter.MessageId);
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
            parameters.Add("TaskId", filter.TaskId);
        }

        parameters.Add("Limit", filter.Limit);

        var records = (await connection.QueryAsync<TakeoverRecord>(
            $"""
            SELECT {TakeoverRecord.Columns}
            FROM agent_takeovers AS t
            {(where.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", where)}")}
            ORDER BY t.created_at DESC, t.id DESC
            LIMIT COALESCE(@Limit, -1);
            """,
            parameters)).ToArray();

        if (records.Length == 0)
        {
            return records;
        }

        var itemParameters = new DynamicParameters();
        var itemNames = new string[records.Length];

        for (var index = 0; index < records.Length; index++)
        {
            var name = $"TakeoverId{index}";
            itemNames[index] = $"@{name}";
            itemParameters.Add(name, records[index].Id);
        }

        var itemRows = await connection.QueryAsync<TakeoverItemRow>(
            $"""
            SELECT takeover_id AS TakeoverId, kind AS Kind, item_id AS ItemId
            FROM agent_takeover_items
            WHERE takeover_id IN ({string.Join(", ", itemNames)})
            ORDER BY takeover_id, kind, item_id;
            """,
            itemParameters);
        var itemsByTakeoverId = itemRows
            .GroupBy(item => item.TakeoverId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<TakeoverItem>)group
                .Select(item => new TakeoverItem { Kind = item.Kind, ItemId = item.ItemId })
                .ToArray(), StringComparer.Ordinal);

        return records.Select(record => new TakeoverRecord
        {
            Id = record.Id,
            FromActor = record.FromActor,
            ToActor = record.ToActor,
            Actor = record.Actor,
            CreatedAt = record.CreatedAt,
            Forced = record.Forced,
            Role = record.Role,
            Reason = record.Reason,
            Items = itemsByTakeoverId.GetValueOrDefault(record.Id, [])
        }).ToArray();
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
            "SELECT COUNT(*) FROM agent_takeovers",
            transaction: transaction);

        for (var attempt = 0; attempt < MaxIdAttempts; attempt++)
        {
            var id = IdPrefix + CreateIdSuffix(seed, takeoverCount, attempt);
            var exists = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM agent_takeovers WHERE id = @Id",
                new { Id = id },
                transaction);

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
}

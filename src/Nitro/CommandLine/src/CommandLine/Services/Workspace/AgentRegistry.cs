using System.Data.Common;
using System.Globalization;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class AgentRegistry(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database) : IAgentRegistry
{
    public async Task<AgentRecord> RegisterAsync(
        string name,
        string role,
        string client,
        CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var agent = await UpsertWithinTransactionAsync(
            connection, transaction, timeProvider, name, role, client, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return agent;
    }

    public async Task<AgentRecord> AllocateAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var actor = await AgentActorAllocator.AllocateAsync(connection, transaction);
        var agent = await UpsertWithinTransactionAsync(
            connection, transaction, timeProvider, actor, string.Empty, string.Empty, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return agent;
    }

    /// <summary>
    /// The agents upsert <see cref="RegisterAsync"/> applies (sets
    /// last_seen_at to now, implicit to false, and role and client to the
    /// given values), exposed so a caller with its own already-open writer
    /// transaction can upsert an agent identity as part of it: SQLite allows
    /// only one writer transaction per connection, so such a caller cannot
    /// go through <see cref="RegisterAsync"/>, which opens its own.
    /// </summary>
    public static async Task<AgentRecord> UpsertWithinTransactionAsync(
        SqliteConnection connection,
        DbTransaction transaction,
        TimeProvider timeProvider,
        string name,
        string role,
        string client,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var normalizedRole = AgentRole.Normalize(role);
        var normalizedClient = NormalizeClient(client);
        var now = timeProvider.GetUtcNow();

        var row = await connection.QueryFirstAsync<AgentRegistryRow>(
            """
            INSERT INTO agents (name, registered_at, last_seen_at, role, client, implicit)
            VALUES (@name, @now, @now, @role, @client, 0)
            ON CONFLICT (name) DO UPDATE SET
                last_seen_at = @now,
                role = @role,
                client = @client,
                implicit = 0
            RETURNING
                name AS Name,
                role AS Role,
                client AS Client,
                implicit AS Implicit,
                registered_at AS RegisteredAt,
                last_seen_at AS LastSeenAt
            """,
            new { name = normalizedName, now, role = normalizedRole, client = normalizedClient, cancellationToken },
            transaction);

        return row.ToAgentRecord();
    }

    public async Task<AgentRecord> TouchAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var row = await connection.QueryFirstAsync<AgentRegistryRow>(
            """
            INSERT INTO agents (name, registered_at, last_seen_at, role, client, implicit)
            VALUES (@name, @now, @now, '', '', 0)
            ON CONFLICT (name) DO UPDATE SET
                last_seen_at = @now,
                implicit = 0
            RETURNING
                name AS Name,
                role AS Role,
                client AS Client,
                implicit AS Implicit,
                registered_at AS RegisteredAt,
                last_seen_at AS LastSeenAt
            """,
            new { name = normalizedName, now, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return row.ToAgentRecord();
    }

    public async Task<AgentRecord?> GetAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        var row = await connection.QueryFirstOrDefaultAsync<AgentRegistryRow>(
            $"SELECT {AgentRecord.Columns} FROM agents WHERE name = @name",
            new { name = normalizedName, cancellationToken });

        return row?.ToAgentRecord();
    }

    public async Task<AgentRecord> EnsureImplicitAsync(
        string name,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // The no-op DO UPDATE (name = excluded.name, always the same value)
        // exists only so RETURNING fires on a conflict too, giving a single
        // round trip that both creates a missing row and fetches an
        // existing one, without touching any column of an existing row.
        var row = await connection.QueryFirstAsync<AgentRegistryRow>(
            """
            INSERT INTO agents (name, registered_at, last_seen_at, role, client, implicit)
            VALUES (@name, @now, @now, '', '', 1)
            ON CONFLICT (name) DO UPDATE SET name = excluded.name
            RETURNING
                name AS Name,
                role AS Role,
                client AS Client,
                implicit AS Implicit,
                registered_at AS RegisteredAt,
                last_seen_at AS LastSeenAt
            """,
            new { name = normalizedName, now, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

        return row.ToAgentRecord();
    }

    public async Task<IReadOnlyList<AgentRecord>> ListAsync(
        string? role,
        DateTimeOffset? staleBefore,
        CancellationToken cancellationToken)
    {
        var normalizedRole = role is null ? null : AgentRole.Normalize(role);

        await using var connection = await ConnectAsync(cancellationToken);

        if (normalizedRole is null && staleBefore is null)
        {
            var rows = await connection.QueryAsync<AgentRegistryRow>(
                $"SELECT {AgentRecord.Columns} FROM agents ORDER BY name");

            return rows.Select(r => r.ToAgentRecord()).ToList();
        }

        if (normalizedRole is not null && staleBefore is null)
        {
            var rows = await connection.QueryAsync<AgentRegistryRow>(
                $"SELECT {AgentRecord.Columns} FROM agents WHERE role = @role ORDER BY name",
                new { role = normalizedRole, cancellationToken });

            return rows.Select(r => r.ToAgentRecord()).ToList();
        }

        if (normalizedRole is null)
        {
            var rows = await connection.QueryAsync<AgentRegistryRow>(
                $"""
                SELECT {AgentRecord.Columns} FROM agents
                WHERE last_seen_at < @staleBefore
                ORDER BY name
                """,
                new { staleBefore, cancellationToken });

            return rows.Select(r => r.ToAgentRecord()).ToList();
        }

        var filteredRows = await connection.QueryAsync<AgentRegistryRow>(
            $"""
            SELECT {AgentRecord.Columns} FROM agents
            WHERE role = @role AND last_seen_at < @staleBefore
            ORDER BY name
            """,
            new { role = normalizedRole, staleBefore, cancellationToken });

        return filteredRows.Select(r => r.ToAgentRecord()).ToList();
    }

    /// <summary>
    /// Trims and lowercases the given value the same way
    /// <see cref="AgentRole.Normalize"/> does. A null or whitespace-only
    /// value normalizes to the empty string; like a role, a client carries
    /// no character restriction.
    /// </summary>
    private static string NormalizeClient(string? client) => (client ?? string.Empty).Trim().ToLowerInvariant();

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException(
                "No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }

    // Internal, not private: Dapper.AOT's generated interceptors live
    // outside AgentRegistry and cannot reference a private nested type, so a
    // private row type would silently fall back to Dapper's reflection-emit
    // deserializer.
    internal sealed class AgentRegistryRow
    {
        public required string Name { get; init; }
        public required string Role { get; init; }
        public required string Client { get; init; }
        public required bool Implicit { get; init; }
        public required string RegisteredAt { get; init; }
        public required string LastSeenAt { get; init; }

        public AgentRecord ToAgentRecord() => new()
        {
            Name = Name,
            Role = Role,
            Client = Client,
            Implicit = Implicit,
            RegisteredAt = DateTimeOffset.Parse(RegisteredAt, CultureInfo.InvariantCulture),
            LastSeenAt = DateTimeOffset.Parse(LastSeenAt, CultureInfo.InvariantCulture)
        };
    }
}

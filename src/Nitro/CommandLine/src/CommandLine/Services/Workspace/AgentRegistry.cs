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
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var normalizedRole = AgentRole.Normalize(role);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var row = await connection.QueryFirstAsync<AgentRegistryRow>(
            """
            INSERT INTO agents (name, registered_at, last_seen_at, role, implicit)
            VALUES (@name, @now, @now, @role, 0)
            ON CONFLICT (name) DO UPDATE SET
                last_seen_at = @now,
                role = @role,
                implicit = 0
            RETURNING
                name AS Name,
                role AS Role,
                implicit AS Implicit,
                registered_at AS RegisteredAt,
                last_seen_at AS LastSeenAt
            """,
            new { name = normalizedName, now, role = normalizedRole, cancellationToken },
            transaction);

        await transaction.CommitAsync(cancellationToken);

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
            INSERT INTO agents (name, registered_at, last_seen_at, role, implicit)
            VALUES (@name, @now, @now, '', 0)
            ON CONFLICT (name) DO UPDATE SET
                last_seen_at = @now,
                implicit = 0
            RETURNING
                name AS Name,
                role AS Role,
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
        public required bool Implicit { get; init; }
        public required string RegisteredAt { get; init; }
        public required string LastSeenAt { get; init; }

        public AgentRecord ToAgentRecord() => new()
        {
            Name = Name,
            Role = Role,
            Implicit = Implicit,
            RegisteredAt = DateTimeOffset.Parse(RegisteredAt, CultureInfo.InvariantCulture),
            LastSeenAt = DateTimeOffset.Parse(LastSeenAt, CultureInfo.InvariantCulture)
        };
    }
}

using ChilliCream.Nitro.CommandLine.Services.Mail;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

internal sealed class AgentStore(
    IFileSystem fileSystem,
    TimeProvider timeProvider,
    AgentDatabase database) : IAgentStore
{
    public async Task<AgentRow> LoginAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        var name = await AgentActorAllocator.AllocateAsync(connection, transaction);

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            $"""
            INSERT INTO agents (name, registered_at, started_at, last_seen_at)
            VALUES (@name, @now, @now, @now)
            RETURNING {AgentRow.Columns};
            """;
        command.Parameters.AddWithValue("@name", name);
        command.Parameters.AddWithValue("@now", now);

        var row = await ReadFirstAsync(command, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return row;
    }

    public async Task<AgentSessionStartResult> StartSessionAsync(
        AgentSessionStartRequest request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        var existing = await FindBySessionWithinTransactionAsync(
            connection, transaction, request.Harness, request.SessionId, cancellationToken);

        if (existing?.IsDeleted == true)
        {
            await transaction.CommitAsync(cancellationToken);
            return AgentSessionStartResult.Ignored;
        }

        if (existing is not null)
        {
            await using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText =
                $"""
                UPDATE agents SET
                    ended_at = NULL,
                    harness_version = CASE WHEN @harnessVersion <> '' THEN @harnessVersion ELSE harness_version END,
                    cwd = @cwd,
                    workspace_path = @workspacePath,
                    endpoint_kind = @endpointKind,
                    endpoint_addr = @endpointAddr,
                    endpoint_secret = @endpointSecret,
                    last_seen_at = @now
                WHERE harness = @harness AND session_id = @sessionId
                RETURNING {AgentRow.Columns};
                """;
            AddSessionRequestParameters(updateCommand, request, now);

            var reused = await ReadFirstAsync(updateCommand, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return AgentSessionStartResult.Reused(reused);
        }

        var name = await AgentActorAllocator.AllocateAsync(connection, transaction);

        await using var insertCommand = connection.CreateCommand();
        insertCommand.Transaction = transaction;
        insertCommand.CommandText =
            $"""
            INSERT INTO agents (
                name, role, harness, harness_version, session_id, cwd, workspace_path,
                registered_at, started_at, last_seen_at, endpoint_kind, endpoint_addr, endpoint_secret)
            VALUES (
                @name, '', @harness, @harnessVersion, @sessionId, @cwd, @workspacePath,
                @now, @now, @now, @endpointKind, @endpointAddr, @endpointSecret)
            RETURNING {AgentRow.Columns};
            """;
        insertCommand.Parameters.AddWithValue("@name", name);
        AddSessionRequestParameters(insertCommand, request, now);

        var minted = await ReadFirstAsync(insertCommand, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return AgentSessionStartResult.Minted(minted);
    }

    public async Task<bool> TouchSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE agents SET last_seen_at = @now "
            + "WHERE harness = @harness AND session_id = @sessionId AND deleted_at IS NULL",
            new { now, harness, sessionId, cancellationToken });

        return rowsAffected > 0;
    }

    public async Task<bool> TouchAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE agents SET last_seen_at = @now WHERE name = @name AND deleted_at IS NULL",
            new { now, name = normalizedName, cancellationToken });

        return rowsAffected > 0;
    }

    public async Task<bool> EndSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE agents SET ended_at = @now "
            + "WHERE harness = @harness AND session_id = @sessionId AND deleted_at IS NULL",
            new { now, harness, sessionId, cancellationToken });

        return rowsAffected > 0;
    }

    public async Task<AgentRow?> SetRoleAsync(string name, string role, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var normalizedRole = AgentRole.Normalize(role);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"""
            UPDATE agents SET role = @role, last_seen_at = @now
            WHERE name = @name AND deleted_at IS NULL
            RETURNING {AgentRow.Columns};
            """;
        command.Parameters.AddWithValue("@role", normalizedRole);
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@name", normalizedName);

        return await ReadFirstOrDefaultAsync(command, cancellationToken);
    }

    public async Task<AgentRow?> FindAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE name = @name";
        command.Parameters.AddWithValue("@name", normalizedName);

        return await ReadFirstOrDefaultAsync(command, cancellationToken);
    }

    public async Task<AgentRow?> FindBySessionAsync(
        string harness, string sessionId, CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        return await FindBySessionWithinTransactionAsync(connection, null, harness, sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<AgentRow>> ListAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE deleted_at IS NULL";

        return await ReadAllAsync(command, cancellationToken);
    }

    private static async Task<AgentRow?> FindBySessionWithinTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        string harness,
        string sessionId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE harness = @harness AND session_id = @sessionId";
        command.Parameters.AddWithValue("@harness", harness);
        command.Parameters.AddWithValue("@sessionId", sessionId);

        return await ReadFirstOrDefaultAsync(command, cancellationToken);
    }

    private static void AddSessionRequestParameters(
        SqliteCommand command, AgentSessionStartRequest request, DateTimeOffset now)
    {
        command.Parameters.AddWithValue("@harness", request.Harness);
        command.Parameters.AddWithValue("@sessionId", request.SessionId);
        command.Parameters.AddWithValue("@harnessVersion", request.HarnessVersion);
        command.Parameters.AddWithValue("@cwd", request.Cwd);
        command.Parameters.AddWithValue("@workspacePath", request.WorkspacePath);
        command.Parameters.AddWithValue("@endpointKind", request.EndpointKind);
        command.Parameters.AddWithValue("@endpointAddr", request.EndpointAddr);
        command.Parameters.AddWithValue("@endpointSecret", (object?)request.EndpointSecret ?? DBNull.Value);
        command.Parameters.AddWithValue("@now", now);
    }

    private static async Task<AgentRow> ReadFirstAsync(SqliteCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return AgentRow.ReadFrom(reader);
    }

    private static async Task<AgentRow?> ReadFirstOrDefaultAsync(
        SqliteCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? AgentRow.ReadFrom(reader) : null;
    }

    private static async Task<IReadOnlyList<AgentRow>> ReadAllAsync(
        SqliteCommand command, CancellationToken cancellationToken)
    {
        var rows = new List<AgentRow>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(AgentRow.ReadFrom(reader));
        }

        return rows;
    }

    private async Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken)
    {
        var workspaceDirectory = AgentWorkspace.Find(fileSystem, fileSystem.GetCurrentDirectory())
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

        return await database.ConnectAsync(workspaceDirectory, cancellationToken);
    }
}

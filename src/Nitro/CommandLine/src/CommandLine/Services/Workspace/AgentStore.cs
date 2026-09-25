using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
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
        EnsureAgentHarness(request.Harness);

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
        EnsureAgentHarness(harness);

        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agents SET last_seen_at = @now "
                + "WHERE harness = @harness AND session_id = @sessionId AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@harness", harness);
        command.Parameters.AddWithValue("@sessionId", sessionId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    public async Task<bool> TouchAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE agents SET last_seen_at = @now WHERE name = @name AND deleted_at IS NULL",
                new { now, name = normalizedName });

        return rowsAffected > 0;
    }

    public async Task<bool> EndSessionAsync(string harness, string sessionId, CancellationToken cancellationToken)
    {
        EnsureAgentHarness(harness);

        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE agents SET ended_at = @now "
                + "WHERE harness = @harness AND session_id = @sessionId AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@harness", harness);
        command.Parameters.AddWithValue("@sessionId", sessionId);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

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
        EnsureAgentHarness(harness);

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

    public async Task<bool> SetEndpointAsync(
        string name,
        string endpointKind,
        string endpointAddr,
        string? endpointSecret,
        CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(deferred: false);

        await using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText = "SELECT harness FROM agents WHERE name = @name AND deleted_at IS NULL";
        selectCommand.Parameters.AddWithValue("@name", normalizedName);

        var harnessResult = await selectCommand.ExecuteScalarAsync(cancellationToken);

        if (harnessResult is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var harness = harnessResult is DBNull ? string.Empty : (string)harnessResult;
        var (kind, addr, secret) = EndpointAddress.Normalize(harness, endpointKind, endpointAddr, endpointSecret);

        await using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText =
            "UPDATE agents SET endpoint_kind = @kind, endpoint_addr = @addr, endpoint_secret = @secret "
            + "WHERE name = @name AND deleted_at IS NULL";
        updateCommand.Parameters.AddWithValue("@kind", kind);
        updateCommand.Parameters.AddWithValue("@addr", addr);
        updateCommand.Parameters.AddWithValue("@secret", (object?)secret ?? DBNull.Value);
        updateCommand.Parameters.AddWithValue("@name", normalizedName);

        await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<int> ResetBlockBudgetAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE agents SET block_budget_used = 0 WHERE name = @name AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@name", normalizedName);

        await command.ExecuteNonQueryAsync(cancellationToken);

        return 0;
    }

    public async Task<int> IncrementBlockBudgetAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = transaction;
        updateCommand.CommandText =
            "UPDATE agents SET block_budget_used = block_budget_used + 1 "
                + "WHERE name = @name AND deleted_at IS NULL";
        updateCommand.Parameters.AddWithValue("@name", normalizedName);

        var rowsAffected = await updateCommand.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return 0;
        }

        await using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText = "SELECT block_budget_used FROM agents WHERE name = @name";
        selectCommand.Parameters.AddWithValue("@name", normalizedName);

        var updated = (long)(await selectCommand.ExecuteScalarAsync(cancellationToken))!;

        await transaction.CommitAsync(cancellationToken);

        return (int)updated;
    }

    public async Task<bool> TryClaimPingCooldownAsync(
        string name, TimeSpan cooldown, string attemptId, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();
        var cutoff = now - cooldown;

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            """
                UPDATE agents SET
                    last_ping_at = @now,
                    last_ping_attempt = @attemptId,
                    last_ping_result = NULL,
                    last_ping_detail = NULL
                WHERE name = @name AND deleted_at IS NULL
                    AND (last_ping_at IS NULL OR last_ping_at <= @cutoff);
                """,
                new { now, attemptId, name = normalizedName, cutoff });

        return rowsAffected > 0;
    }

    public async Task WritePingResultAsync(
        string name, string attemptId, string result, string? detail, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await connection.ExecuteAsync(
            "UPDATE agents SET last_ping_result = @result, last_ping_detail = @detail "
                + "WHERE name = @name AND deleted_at IS NULL AND last_ping_attempt = @attemptId",
                new { result, detail, name = normalizedName, attemptId });
    }

    public Task ArmAnnouncementAsync(string name, CancellationToken cancellationToken)
        => SetAgentFlagAsync(name, "announcement_pending", value: true, cancellationToken);

    public Task<bool> ClaimAnnouncementAsync(string name, CancellationToken cancellationToken)
        => ClaimAgentFlagAsync(name, "announcement_pending", cancellationToken);

    public Task<bool> IsAnnouncementPendingAsync(string name, CancellationToken cancellationToken)
        => PeekAgentFlagAsync(name, "announcement_pending", cancellationToken);

    public Task RearmIdlePushAsync(string name, CancellationToken cancellationToken)
        => SetAgentFlagAsync(name, "idle_push_armed", value: true, cancellationToken);

    public Task<bool> ClaimIdlePushAsync(string name, CancellationToken cancellationToken)
        => ClaimAgentFlagAsync(name, "idle_push_armed", cancellationToken);

    public async Task<bool> RecordHarnessVersionAsync(
        string name, string harnessVersion, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(
            "UPDATE agents SET harness_version = @harnessVersion "
                + "WHERE name = @name AND deleted_at IS NULL",
                new { harnessVersion, name = normalizedName });

        return rowsAffected > 0;
    }

    public async Task<bool> DeleteAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);
        var now = timeProvider.GetUtcNow();
        var reason = $"Agent '{normalizedName}' was deleted";

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        var deleted = await DeleteAgentRowWithinTransactionAsync(
            connection, transaction, normalizedName, now, cancellationToken);

        if (!deleted)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        await DeleteAgentSideRowsWithinTransactionAsync(connection, transaction, normalizedName, cancellationToken);

        await TaskStore.ReleaseAssigneeWithinTransactionAsync(
            connection, transaction, normalizedName, reason, now, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;
    }

    public async Task<int> DeleteInactiveAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        await using var connection = await ConnectAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await using var selectCommand = connection.CreateCommand();
        selectCommand.Transaction = transaction;
        selectCommand.CommandText = $"SELECT {AgentRow.Columns} FROM agents WHERE deleted_at IS NULL";

        var candidates = await ReadAllAsync(selectCommand, cancellationToken);

        var deletedCount = 0;

        foreach (var candidate in candidates)
        {
            var state = AgentStateResolver.Resolve(candidate, now);

            if (state != AgentState.Offline && state != AgentState.Idle)
            {
                continue;
            }

            var deleted = await DeleteAgentRowWithinTransactionAsync(
                connection, transaction, candidate.Name, now, cancellationToken);

            if (!deleted)
            {
                continue;
            }

            await DeleteAgentSideRowsWithinTransactionAsync(
                connection, transaction, candidate.Name, cancellationToken);

            await TaskStore.ReleaseAssigneeWithinTransactionAsync(
                connection, transaction, candidate.Name, $"Agent '{candidate.Name}' was deleted", now,
                cancellationToken);

            deletedCount++;
        }

        await transaction.CommitAsync(cancellationToken);

        return deletedCount;
    }

    /// <summary>
    /// Stamps <c>deleted_at</c> and clears the agent's endpoint and transient state.
    /// Returns false, changing nothing, when no matching non-deleted row exists.
    /// </summary>
    private static async Task<bool> DeleteAgentRowWithinTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            UPDATE agents SET
                deleted_at = @now,
                endpoint_kind = 'none',
                endpoint_addr = '',
                endpoint_secret = NULL,
                last_ping_at = NULL,
                last_ping_attempt = NULL,
                last_ping_result = NULL,
                last_ping_detail = NULL,
                announcement_pending = 0,
                idle_push_armed = 0,
                block_budget_used = 0
            WHERE name = @name AND deleted_at IS NULL
            """;
        command.Parameters.AddWithValue("@now", now);
        command.Parameters.AddWithValue("@name", name);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Removes the agent's wake and ping side rows: its delivery reservations, ping gate,
    /// wake outbox entry, owned wake batches, and wake target entries.
    /// </summary>
    private static async Task DeleteAgentSideRowsWithinTransactionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string name,
        CancellationToken cancellationToken)
    {
        string[] statements =
        [
            "DELETE FROM agent_deliveries WHERE agent = @name",
            "DELETE FROM agent_ping_gates WHERE agent = @name",
            "DELETE FROM mail_wake_targets WHERE agent = @name",
            "DELETE FROM mail_wake_batches WHERE actor = @name",
            "DELETE FROM mail_wake_outbox WHERE actor = @name"
        ];

        foreach (var statement in statements)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = statement;
            command.Parameters.AddWithValue("@name", name);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Sets the named flag for the matching agent; a missing or deleted agent is unchanged.
    /// <paramref name="column"/> must be a trusted agent flag column name.
    /// </summary>
    private async Task SetAgentFlagAsync(
        string name, string column, bool value, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE agents SET {column} = @value WHERE name = @name AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@value", value ? 1 : 0);
        command.Parameters.AddWithValue("@name", normalizedName);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Clears the named flag for the matching agent and returns whether it was set.
    /// <paramref name="column"/> must be a trusted agent flag column name.
    /// </summary>
    private async Task<bool> ClaimAgentFlagAsync(string name, string column, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            $"UPDATE agents SET {column} = 0 WHERE name = @name AND deleted_at IS NULL AND {column} = 1";
        command.Parameters.AddWithValue("@name", normalizedName);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }

    /// <summary>
    /// Returns whether the named flag is set for the matching agent, or false when none
    /// matches. <paramref name="column"/> must be a trusted agent flag column name.
    /// </summary>
    private async Task<bool> PeekAgentFlagAsync(string name, string column, CancellationToken cancellationToken)
    {
        var normalizedName = MailAgentName.Normalize(name);

        await using var connection = await ConnectAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT {column} FROM agents WHERE name = @name AND deleted_at IS NULL";
        command.Parameters.AddWithValue("@name", normalizedName);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        return result is not null && (long)result != 0;
    }

    private static void EnsureAgentHarness(string harness)
    {
        if (!AgentSessionHarness.IsAgentHarness(harness))
        {
            throw ThrowHelper.UnknownAgentHarness(harness);
        }
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

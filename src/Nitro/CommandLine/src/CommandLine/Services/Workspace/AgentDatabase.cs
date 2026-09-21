using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Opens, initializes, and upgrades the shared agent workspace database.
/// </summary>
internal sealed class AgentDatabase
{
    /// <summary>
    /// The current unified schema version.
    /// </summary>
    public const int CurrentVersion = 14;

    /// <summary>
    /// Schema versions <see cref="InitializeAsync"/> upgrades in place
    /// instead of rejecting.
    /// </summary>
    private static readonly int[] s_upgradableVersions = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13];

    /// <summary>
    /// True for a schema version <see cref="InitializeAsync"/> upgrades in
    /// place instead of rejecting.
    /// </summary>
    public static bool IsUpgradableVersion(long version) => Array.IndexOf(s_upgradableVersions, (int)version) >= 0;

    static AgentDatabase() => SQLitePCL.Batteries_V2.Init();

    /// <summary>
    /// Opens a connection to a new or existing workspace database, applying
    /// the current schema and any pending in-place upgrades, and returns the
    /// open connection. Throws <see cref="ExitException"/> when the existing
    /// database's version is anything other than 0, one of
    /// <see cref="s_upgradableVersions"/>, or <see cref="CurrentVersion"/>.
    /// </summary>
    public async Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(
            AgentWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        long version;

        try
        {
            version = await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");

            ValidateVersionForInitialize(version);

            await ConfigureAcceptedConnectionAsync(connection, cancellationToken);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }

        // The constraint rebuild runs in its own transaction.
        await RebuildAgentSessionsCheckConstraintIfStaleAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(TaskStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentRegistrySchema.Create, transaction: transaction);
        await connection.ExecuteAsync(MailStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentSessionSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentSessionIdentitySchema.Create, transaction: transaction);

        await connection.ExecuteAsync(MailWakeSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(SessionPingGateSchema.Create, transaction: transaction);

        await connection.ExecuteAsync(MemoryStoreSchema.Create, transaction: transaction);

        await connection.ExecuteAsync(TakeoverLedgerSchema.Create, transaction: transaction);

        // Imports legacy markdown entries whose ids are absent from the database.
        await MemoryMarkdownImport.ImportAsync(
            connection, transaction, workspaceDirectory, cancellationToken);

        await RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync(connection, transaction);
        await RebuildSessionPingGatesHarnessCheckConstraintIfStaleAsync(connection, transaction);
        await RebuildAgentSessionIdentitiesHarnessCheckConstraintIfStaleAsync(connection, transaction);

        await UpgradeAgentsTableAsync(connection, transaction);

        await UpgradeAgentSessionsMetadataColumnsAsync(connection, transaction);

        if (version == 8)
        {
            await ResetLegacyAgentStateAsync(connection, transaction);
        }

        await connection.ExecuteAsync(
            $"""PRAGMA user_version = {CurrentVersion};""", transaction: transaction);

        await transaction.CommitAsync(cancellationToken);

        // The constraint rebuild runs after the schema transaction commits.
        await RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync(connection, cancellationToken);

        return connection;
    }

    private static Task ResetLegacyAgentStateAsync(
        SqliteConnection connection,
        DbTransaction transaction)
        => connection.ExecuteAsync(
            """
            DELETE FROM mail_wake_targets;
            DELETE FROM mail_wake_batches;
            DELETE FROM mail_wake_outbox;
            DELETE FROM mail_wake_daemons;
            DELETE FROM session_ping_gates;
            DELETE FROM session_deliveries;
            DELETE FROM ping_leases;
            DELETE FROM message_recipients;
            DELETE FROM messages;
            DELETE FROM agent_sessions;
            DELETE FROM agent_session_identities;
            DELETE FROM agents;
            """,
            transaction: transaction);

    /// <summary>
    /// Adds any missing role, implicit, and client columns to the agents table.
    /// </summary>
    private static async Task UpgradeAgentsTableAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var columns = (await connection.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('agents');", transaction: transaction))
            .ToHashSet(StringComparer.Ordinal);

        if (!columns.Contains("role"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agents ADD COLUMN role TEXT NOT NULL DEFAULT '';",
                transaction: transaction);
        }

        if (!columns.Contains("implicit"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agents ADD COLUMN implicit INTEGER NOT NULL DEFAULT 0 CHECK (implicit IN (0, 1));",
                transaction: transaction);
        }

        if (!columns.Contains("client"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agents ADD COLUMN client TEXT NOT NULL DEFAULT '';",
                transaction: transaction);
        }
    }

    /// <summary>
    /// Adds missing session metadata and delivery-state columns.
    /// </summary>
    private static async Task UpgradeAgentSessionsMetadataColumnsAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var columns = (await connection.QueryAsync<string>(
                "SELECT name FROM pragma_table_info('agent_sessions');", transaction: transaction))
            .ToHashSet(StringComparer.Ordinal);

        if (!columns.Contains("role"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agent_sessions ADD COLUMN role TEXT NOT NULL DEFAULT '';",
                transaction: transaction);
        }

        if (!columns.Contains("harness_version"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agent_sessions ADD COLUMN harness_version TEXT NOT NULL DEFAULT '';",
                transaction: transaction);
        }

        if (!columns.Contains("endpoint_secret"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agent_sessions ADD COLUMN endpoint_secret TEXT NULL;",
                transaction: transaction);
        }

        if (!columns.Contains("announcement_pending"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agent_sessions ADD COLUMN announcement_pending INTEGER NOT NULL DEFAULT 0 "
                + "CHECK (announcement_pending IN (0, 1));",
                transaction: transaction);
        }

        if (!columns.Contains("idle_push_armed"))
        {
            await connection.ExecuteAsync(
                "ALTER TABLE agent_sessions ADD COLUMN idle_push_armed INTEGER NOT NULL DEFAULT 0 "
                + "CHECK (idle_push_armed IN (0, 1));",
                transaction: transaction);
        }
    }

    /// <summary>
    /// Rebuilds <c>agent_sessions</c> in place when its stamped CHECK
    /// constraint on <c>last_ping_result</c> predates <c>unsupported</c>. A
    /// no-op when the constraint already lists <c>unsupported</c> or when
    /// the table does not exist yet.
    /// </summary>
    private static async Task RebuildAgentSessionsCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions';");

        if (createTableSql is null || createTableSql.Contains("'unsupported'", StringComparison.Ordinal))
        {
            return;
        }

        await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            const string rebuildTableName = "agent_sessions_check_rebuild";

            await connection.ExecuteAsync(
                $"""DROP TABLE IF EXISTS "{rebuildTableName}";""", transaction: transaction);
            await connection.ExecuteAsync(
                AgentSessionSchema.CreateAgentSessionsTable(rebuildTableName), transaction: transaction);
            await connection.ExecuteAsync(
                $"""
                INSERT INTO "{rebuildTableName}" (
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail
                )
                SELECT
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail
                FROM agent_sessions;
                """,
                transaction: transaction);
            await connection.ExecuteAsync("DROP TABLE agent_sessions;", transaction: transaction);
            await connection.ExecuteAsync(
                $"""ALTER TABLE "{rebuildTableName}" RENAME TO agent_sessions;""", transaction: transaction);

            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_agent_sessions_name ON agent_sessions (agent_name);",
                transaction: transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
        }
    }

    /// <summary>
    /// Rebuilds <c>agent_sessions</c> in place when its stamped CHECK
    /// constraints on <c>harness</c> or <c>endpoint_kind</c> predate the
    /// current accepted values, or when it still carries the retired
    /// <c>pid</c> column. A no-op when the table is already current or does
    /// not exist yet.
    /// </summary>
    private static async Task RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions';");

        if (createTableSql is null
            || (createTableSql.Contains("'opencode'", StringComparison.Ordinal)
                && createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
                && createTableSql.Contains("'opencode-server'", StringComparison.Ordinal)
                && createTableSql.Contains("endpoint_secret", StringComparison.Ordinal)
                && !createTableSql.Contains("pid INTEGER", StringComparison.Ordinal)))
        {
            return;
        }

        await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            const string rebuildTableName = "agent_sessions_harness_rebuild";

            await connection.ExecuteAsync(
                $"""DROP TABLE IF EXISTS "{rebuildTableName}";""", transaction: transaction);
            await connection.ExecuteAsync(
                AgentSessionSchema.CreateAgentSessionsTable(rebuildTableName), transaction: transaction);
            await connection.ExecuteAsync(
                $"""
                INSERT INTO "{rebuildTableName}" (
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, endpoint_secret, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail,
                    role, harness_version
                )
                SELECT
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, endpoint_secret, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail,
                    role, harness_version
                FROM agent_sessions;
                """,
                transaction: transaction);
            await connection.ExecuteAsync("DROP TABLE agent_sessions;", transaction: transaction);
            await connection.ExecuteAsync(
                $"""ALTER TABLE "{rebuildTableName}" RENAME TO agent_sessions;""", transaction: transaction);

            await connection.ExecuteAsync(
                "CREATE INDEX IF NOT EXISTS idx_agent_sessions_name ON agent_sessions (agent_name);",
                transaction: transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
        }
    }

    /// <summary>
    /// Rebuilds <c>agent_session_identities</c> when its harness CHECK
    /// constraint predates the <c>opencode</c> harness value.
    /// </summary>
    private static async Task RebuildAgentSessionIdentitiesHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'agent_session_identities';",
            transaction: transaction);

        if (createTableSql is null || createTableSql.Contains("'opencode'", StringComparison.Ordinal))
        {
            return;
        }

        const string rebuildTableName = "agent_session_identities_harness_rebuild";

        await connection.ExecuteAsync(
            $"""DROP TABLE IF EXISTS "{rebuildTableName}";""", transaction: transaction);
        await connection.ExecuteAsync(
            AgentSessionIdentitySchema.CreateAgentSessionIdentitiesTable(rebuildTableName), transaction: transaction);
        await connection.ExecuteAsync(
            $"""
            INSERT INTO "{rebuildTableName}" (
                harness, session_id, actor, role, actor_revision, created_at, last_seen_at
            )
            SELECT
                harness, session_id, actor, role, actor_revision, created_at, last_seen_at
            FROM agent_session_identities;
            """,
            transaction: transaction);
        await connection.ExecuteAsync("DROP TABLE agent_session_identities;", transaction: transaction);
        await connection.ExecuteAsync(
            $"""ALTER TABLE "{rebuildTableName}" RENAME TO agent_session_identities;""", transaction: transaction);
        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_agent_session_identities_actor "
            + "ON agent_session_identities (actor);",
            transaction: transaction);
    }

    /// <summary>
    /// Rebuilds <c>mail_wake_targets</c> in place when its stamped
    /// <c>harness</c> CHECK constraint predates the current accepted values,
    /// or when it still carries the retired <c>pid</c> column. A no-op when
    /// the table is already current, or when it does not exist yet.
    /// </summary>
    private static async Task RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'mail_wake_targets';",
            transaction: transaction);

        if (createTableSql is null
            || (createTableSql.Contains("'opencode'", StringComparison.Ordinal)
                && createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
                && !createTableSql.Contains("pid INTEGER", StringComparison.Ordinal)))
        {
            return;
        }

        const string rebuildTableName = "mail_wake_targets_harness_rebuild";

        await connection.ExecuteAsync(
            $"""DROP TABLE IF EXISTS "{rebuildTableName}";""", transaction: transaction);
        await connection.ExecuteAsync(
            MailWakeSchema.CreateMailWakeTargetsTable(rebuildTableName), transaction: transaction);
        await connection.ExecuteAsync(
            $"""
            INSERT INTO "{rebuildTableName}" (
                batch_id, harness, session_id, host,
                status, offered_generation, accepted_generation, last_error, updated_at
            )
            SELECT
                batch_id, harness, session_id, host,
                status, offered_generation, accepted_generation, last_error, updated_at
            FROM mail_wake_targets;
            """,
            transaction: transaction);
        await connection.ExecuteAsync("DROP TABLE mail_wake_targets;", transaction: transaction);
        await connection.ExecuteAsync(
            $"""ALTER TABLE "{rebuildTableName}" RENAME TO mail_wake_targets;""", transaction: transaction);
    }

    /// <summary>
    /// Rebuilds <c>session_ping_gates</c> in place when its stamped
    /// <c>harness</c> CHECK constraint predates the current accepted values,
    /// or when it still carries the retired <c>pid</c> column. A no-op when
    /// the table is already current, or when it does not exist yet.
    /// </summary>
    private static async Task RebuildSessionPingGatesHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'session_ping_gates';",
            transaction: transaction);

        if (createTableSql is null
            || (createTableSql.Contains("'opencode'", StringComparison.Ordinal)
                && createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
                && !createTableSql.Contains("pid INTEGER", StringComparison.Ordinal)))
        {
            return;
        }

        const string rebuildTableName = "session_ping_gates_harness_rebuild";

        await connection.ExecuteAsync(
            $"""DROP TABLE IF EXISTS "{rebuildTableName}";""", transaction: transaction);
        await connection.ExecuteAsync(
            SessionPingGateSchema.CreateSessionPingGatesTable(rebuildTableName), transaction: transaction);
        await connection.ExecuteAsync(
            $"""
            INSERT INTO "{rebuildTableName}" (
                harness, session_id, host, attempt_id, acquired_at, expires_at
            )
            SELECT
                harness, session_id, host, attempt_id, acquired_at, expires_at
            FROM session_ping_gates;
            """,
            transaction: transaction);
        await connection.ExecuteAsync("DROP TABLE session_ping_gates;", transaction: transaction);
        await connection.ExecuteAsync(
            $"""ALTER TABLE "{rebuildTableName}" RENAME TO session_ping_gates;""", transaction: transaction);

        await connection.ExecuteAsync(
            "CREATE INDEX IF NOT EXISTS idx_session_ping_gates_expires ON session_ping_gates (expires_at);",
            transaction: transaction);
    }

    /// <summary>
    /// Opens a connection to the workspace database at the given directory
    /// for normal use. Throws <see cref="ExitException"/> when the
    /// database's schema version is not exactly <see cref="CurrentVersion"/>.
    /// </summary>
    public async Task<SqliteConnection> ConnectAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(
            AgentWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        try
        {
            var version = await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");

            ValidateVersionForConnect(version);

            await ConfigureAcceptedConnectionAsync(connection, cancellationToken);

            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Reads the schema version stamped on the workspace database at the
    /// given directory, without applying or validating anything against it.
    /// </summary>
    public async Task<long> ReadVersionAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(
            AgentWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        return await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");
    }

    private static void ValidateVersionForInitialize(long version)
    {
        if (version > CurrentVersion)
        {
            throw new ExitException(
                "The agent workspace was created by a newer version of the Nitro CLI "
                + $"(schema v{version}, supported up to v{CurrentVersion}). "
                + "Update the CLI to use it.");
        }

        if (version != 0 && !IsUpgradableVersion(version) && version != CurrentVersion)
        {
            throw new ExitException(
                $"The database at this path has schema v{version}, which the unified agent "
                + "workspace does not support.");
        }
    }

    private static void ValidateVersionForConnect(long version)
    {
        if (version > CurrentVersion)
        {
            throw new ExitException(
                "The agent workspace was created by a newer version of the Nitro CLI "
                + $"(schema v{version}, supported up to v{CurrentVersion}). "
                + "Update the CLI to use it.");
        }

        if (version != CurrentVersion)
        {
            throw new AgentWorkspaceSchemaMismatchException(
                $"The agent workspace database has schema v{version}, expected v{CurrentVersion}. "
                + "Run `nitro agent init` to migrate it.");
        }
    }

    private static async Task<SqliteConnection> OpenAsync(
        string databasePath,
        CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");

        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static Task ConfigureAcceptedConnectionAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
        => connection.ExecuteAsync(
            new CommandDefinition(
                "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;",
                cancellationToken: cancellationToken));
}

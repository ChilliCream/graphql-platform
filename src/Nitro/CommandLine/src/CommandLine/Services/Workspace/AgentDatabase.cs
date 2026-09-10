using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
using ChilliCream.Nitro.CommandLine.Services.Memory;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Opens and initializes the unified agent workspace database that backs
/// both the task tracker and the mail feature: one file, one schema
/// composed of both feature's tables, and one PRAGMA user_version.
/// </summary>
internal sealed class AgentDatabase
{
    /// <summary>
    /// The current unified schema version. The task-only and mail-only
    /// databases that predate the shared workspace were each version 1; a
    /// database at a legacy path carrying either of those versions is
    /// migrated, not opened here.
    /// </summary>
    public const int CurrentVersion = 11;

    /// <summary>
    /// Schema versions upgraded in place by <see cref="InitializeAsync"/>
    /// rather than rejected: v2 (before the agents table gained its role and
    /// implicit columns), v3 (after those columns and the client column,
    /// before the v4 <see cref="AgentSessionSchema"/> tables), v4 (before
    /// <c>agent_sessions</c> gained its v5 role, harness_version, and
    /// process_scope columns), v5, v6 (before the v7
    /// <see cref="MailWakeSchema"/> and <see cref="SessionPingGateSchema"/>
    /// tables), and v7 (before <c>agent_sessions</c>', <c>mail_wake_targets</c>'
    /// and <c>session_ping_gates</c>' <c>harness</c> CHECK constraints, and
    /// <c>agent_sessions</c>' <c>endpoint_kind</c> CHECK constraint, accepted
    /// the v8 <c>nitro-board</c> and <c>db-watch</c> values). A v3 database's
    /// agents table already carries every column
    /// <see cref="UpgradeAgentsTableAsync"/> adds, so upgrading it only
    /// means applying the new v4 tables and bumping the stamped version. A
    /// v4 database's <c>agent_sessions</c> table already carries every
    /// column except the two <see cref="UpgradeAgentSessionsMetadataColumnsAsync"/>
    /// adds. A v6 database is missing every v7 table outright, so
    /// upgrading it means only applying the new schema and bumping the
    /// stamped version, the same unconditional <c>CREATE TABLE IF NOT EXISTS</c>
    /// shape the v4 session tables used when they were added: there is no
    /// existing column data to carry forward, so no dedicated upgrade
    /// method is needed for this step. A v7 database's <c>agent_sessions</c>
    /// CHECK constraints predate <c>nitro-board</c>/<c>db-watch</c>, rebuilt
    /// in place by <see cref="RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync"/>.
    /// Its <c>mail_wake_targets</c> and <c>session_ping_gates</c> tables'
    /// <c>harness</c> CHECK constraints predate <c>nitro-board</c> the same
    /// way, rebuilt in place by
    /// <see cref="RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync"/>
    /// and <see cref="RebuildSessionPingGatesHarnessCheckConstraintIfStaleAsync"/>.
    /// The v8-to-v9 upgrade preserves task tables and external memory files,
    /// but resets legacy agent, connection, mail, and wake state so the v9
    /// one-session/one-actor invariant starts from a consistent state. The
    /// v10-to-v11 upgrade adds the memory tables and carries any markdown
    /// memory store found beside the workspace into them; see
    /// <see cref="MemoryMarkdownImport"/>. The
    /// v9-to-v10 upgrade drops the <c>pid</c> and <c>proc_start</c> columns
    /// (and <c>agent_sessions</c>' <c>process_scope</c> and
    /// <c>proc_start_legacy</c>) from all three tables that carried them:
    /// a session is identified by (harness, session_id, host) alone, never
    /// by a process. SQLite cannot drop a primary key column in place, so
    /// each of the three rebuilds that already exist for a stale CHECK
    /// constraint also triggers on a surviving <c>pid</c> column and copies
    /// every row across without it.
    /// </summary>
    private static readonly int[] s_upgradableVersions = [2, 3, 4, 5, 6, 7, 8, 9, 10];

    /// <summary>
    /// True for a schema version <see cref="InitializeAsync"/> upgrades in
    /// place instead of rejecting. Exposed so callers such as `agent init`
    /// can decide, before calling in, whether an existing database on disk
    /// is a plain-init upgrade candidate or requires <c>--force</c>.
    /// </summary>
    public static bool IsUpgradableVersion(long version) => Array.IndexOf(s_upgradableVersions, (int)version) >= 0;

    static AgentDatabase() => SQLitePCL.Batteries_V2.Init();

    /// <summary>
    /// Opens a connection to a new or existing workspace database, rebuilds
    /// <c>agent_sessions</c>' <c>last_ping_result</c> CHECK constraint first
    /// if it predates <c>unsupported</c> (see
    /// <see cref="RebuildAgentSessionsCheckConstraintIfStaleAsync"/>, its own
    /// transaction since it needs foreign key enforcement off), then, in one
    /// further transaction, applies the task, mail, agent registry,
    /// mail-wake, and session-ping-gate schemas, upgrades the agents
    /// table's role, implicit, and client columns and the agent_sessions
    /// table's role and harness_version columns in place when any of them
    /// predate the database on hand, and stamps the
    /// current schema version. Returns the open
    /// connection so callers, including test seeding helpers, can write
    /// against it directly. Throws <see cref="ExitException"/> when the existing
    /// database's version is anything other than 0 (a genuinely new file),
    /// one of <see cref="s_upgradableVersions"/>, or <see cref="CurrentVersion"/>,
    /// checked before either transaction touches the file.
    /// </summary>
    public async Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(
            AgentWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        // Validated before anything else touches the file, including the
        // constraint rebuild below: a database newer than this CLI
        // understands must be rejected untouched, not partially rewritten
        // by a rebuild built against this CLI's own idea of the table's
        // shape.
        var version = await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");

        ValidateVersionForInitialize(version);

        // Must run before the main transaction below starts, and manages its
        // own: PRAGMA foreign_keys can only be toggled when there is no
        // pending transaction on the connection, and rebuilding
        // agent_sessions needs it off for the moment the table
        // session_deliveries holds a cascading foreign key against is
        // dropped, so that drop does not cascade-delete session_deliveries'
        // rows before the replacement table is renamed into place. Not
        // gated on version, for the same reason UpgradeAgentsTableAsync
        // below isn't: 'unsupported' was added to the last_ping_result CHECK
        // constraint without bumping CurrentVersion past 4, so a database
        // already at v4 from before that change still carries the old
        // constraint verbatim (a plain CREATE TABLE IF NOT EXISTS is a no-op
        // against it) and rejects the new value until rebuilt. Detecting and
        // rebuilding unconditionally, the same one-code-path shape as the
        // column upgrades, covers a brand new file (no agent_sessions table
        // yet, so a no-op), an already-current file predating 'unsupported',
        // an upgradable v2 or v3 file (neither has an agent_sessions table
        // yet either), and an upgradable v4 file (has the table, rebuilt or
        // not depending on when it was created) alike, without a
        // version-keyed branch for a change that never had its own version
        // number.
        await RebuildAgentSessionsCheckConstraintIfStaleAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(TaskStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentRegistrySchema.Create, transaction: transaction);
        await connection.ExecuteAsync(MailStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentSessionSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentSessionIdentitySchema.Create, transaction: transaction);

        // v7: every mail-wake and session-ping-gate table is brand new, so
        // (like AgentSessionSchema.Create above) applying it unconditionally
        // with CREATE TABLE IF NOT EXISTS covers a fresh database and every
        // upgradable version alike, without a dedicated column-upgrade
        // method.
        await connection.ExecuteAsync(MailWakeSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(SessionPingGateSchema.Create, transaction: transaction);

        // v11: memory moved out of markdown files and into this database,
        // beside tasks and mail. Brand new tables, so the same unconditional
        // CREATE TABLE IF NOT EXISTS shape covers a fresh database and every
        // upgradable version alike.
        await connection.ExecuteAsync(MemoryStoreSchema.Create, transaction: transaction);

        // Runs against every version, not just v10: the markdown store is
        // detected by its own presence on disk, and the import skips ids the
        // database already carries, so a workspace that has already been
        // carried across is a no-op on every later open.
        await MemoryMarkdownImport.ImportAsync(
            connection, transaction, workspaceDirectory, cancellationToken);

        // v8: mail_wake_targets' and session_ping_gates' harness CHECK
        // constraints predate the nitro-board value on a v7 database, the
        // same gap RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync
        // closes for agent_sessions below. Neither table is the target of a
        // foreign key from another table, unlike agent_sessions, so the
        // rebuild can run inside this transaction without toggling PRAGMA
        // foreign_keys or running in its own transaction afterward.
        await RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync(connection, transaction);
        await RebuildSessionPingGatesHarnessCheckConstraintIfStaleAsync(connection, transaction);

        // Column-by-column, not gated on version == UpgradableVersion: the
        // client column shipped after CurrentVersion was last bumped to 3,
        // so an existing v3 database (this repo's own workspace included)
        // needs the same ALTER a v2 database does. Running it unconditionally,
        // guarded per column, covers a brand new file (CREATE TABLE already
        // has every column, so every check is a no-op), an upgradable v2
        // file, and a v3 file that predates client alike, with one code
        // path instead of a version-keyed branch per column ever added.
        await UpgradeAgentsTableAsync(connection, transaction);

        // Same unconditional, column-checked shape as UpgradeAgentsTableAsync
        // above: every existing v4 database predates these three columns, so
        // this is what actually carries out the v4-to-v5 migration this
        // bead adds.
        await UpgradeAgentSessionsMetadataColumnsAsync(connection, transaction);

        if (version == 8)
        {
            await ResetLegacyAgentStateAsync(connection, transaction);
        }

        await connection.ExecuteAsync(
            $"""PRAGMA user_version = {CurrentVersion};""", transaction: transaction);

        await transaction.CommitAsync(cancellationToken);

        // Run after the transaction above commits, not inside it: like the
        // last_ping_result rebuild above, this needs foreign key enforcement
        // off for the moment agent_sessions is dropped, which PRAGMA
        // foreign_keys cannot do with a transaction pending. Running it here
        // rather than alongside that earlier rebuild also guarantees every
        // column the current schema carries (v5's role and harness_version)
        // already exists on the source table by the time it runs, so every
        // row's real values are copied through unchanged instead of being
        // re-defaulted.
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
    /// Adds the agents table's role, implicit, and client columns when the
    /// database on hand's agents table predates any of them, checked column
    /// by column so this is safe to run against a table that already
    /// carries all three.
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
    /// Adds the <c>agent_sessions</c> table's v5 <c>role</c> and
    /// <c>harness_version</c> columns when the database on hand's table
    /// predates either of them, checked column by column so this is safe to
    /// run against a table that already carries both.
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
    }

    /// <summary>
    /// Rebuilds <c>agent_sessions</c> in place when its stamped CHECK
    /// constraint on <c>last_ping_result</c> predates <c>unsupported</c>,
    /// detected by inspecting the table's own recorded SQL in
    /// <c>sqlite_master</c> rather than the stamped schema version: SQLite
    /// has no ALTER for a CHECK constraint, so an existing database's
    /// constraint stays exactly as first created regardless of how the
    /// schema constant in code has since changed. A no-op when the
    /// constraint already lists <c>unsupported</c> (including every
    /// freshly created table) or when the table does not exist yet.
    /// </summary>
    /// <remarks>
    /// The rebuild follows SQLite's documented procedure for a table other
    /// tables hold a foreign key against (<c>session_deliveries</c>, here):
    /// build the replacement under a fresh name, copy every row across,
    /// drop the live table, then rename the replacement into its place, so
    /// <c>session_deliveries</c>' foreign key (which SQLite resolves by
    /// name, not by renaming it to track a table renamed out from under it)
    /// keeps pointing at a table actually named <c>agent_sessions</c> once
    /// the transaction commits. Foreign key enforcement is turned off for
    /// the duration and run in its own transaction, both required because
    /// dropping the live table while it is still enforced cascade-deletes
    /// every referencing <c>session_deliveries</c> row immediately, before
    /// the replacement exists to take its place; <c>PRAGMA foreign_keys</c>
    /// can only be changed with no transaction pending, so this cannot
    /// share the caller's transaction.
    /// </remarks>
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

            // The table's own indexes were attached to it under the old
            // name and were dropped along with it; agent_sessions itself
            // was never renamed, so session_deliveries' foreign key still
            // resolves.
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
    /// constraints on <c>harness</c> or <c>endpoint_kind</c> predate the v8
    /// <c>nitro-board</c> harness value or <c>db-watch</c> endpoint kind, or
    /// when it still carries the v9 <c>pid</c> column the v10 schema drops,
    /// detected the same way <see cref="RebuildAgentSessionsCheckConstraintIfStaleAsync"/>
    /// detects a stale <c>last_ping_result</c> constraint: by inspecting the
    /// table's own recorded SQL in <c>sqlite_master</c>, since SQLite has no
    /// ALTER for a CHECK constraint. A no-op when the constraints already
    /// list both new values (including every freshly created table, and any
    /// table the rebuild above already recreated under the current column
    /// template) or when the table does not exist yet. Every column the
    /// current schema carries is copied through unchanged rather than
    /// re-defaulted: unlike the rebuild above, this one only ever runs after
    /// every column-upgrade step in <see cref="InitializeAsync"/> has
    /// already applied, so v5's role and harness_version are guaranteed
    /// present on the source table. The columns v10 drops are simply left
    /// out of the copy.
    /// </summary>
    /// <remarks>
    /// Follows the same drop-under-a-fresh-name-then-rename procedure as
    /// <see cref="RebuildAgentSessionsCheckConstraintIfStaleAsync"/>, for the
    /// same reason (a foreign key <c>session_deliveries</c> holds against
    /// this table, and foreign key enforcement must be off for the moment
    /// the live table is dropped): see that method's remarks for the full
    /// rationale. Run in its own transaction with foreign keys off, since
    /// <c>PRAGMA foreign_keys</c> cannot be toggled with a transaction
    /// pending, so this cannot share <see cref="InitializeAsync"/>'s main
    /// transaction and instead runs immediately after it commits.
    /// </remarks>
    private static async Task RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'agent_sessions';");

        if (createTableSql is null
            || (createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
                && createTableSql.Contains("'db-watch'", StringComparison.Ordinal)
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
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail,
                    role, harness_version
                )
                SELECT
                    harness, session_id, agent_name, binding_kind, host,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
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
    /// Rebuilds <c>mail_wake_targets</c> in place when its stamped
    /// <c>harness</c> CHECK constraint predates the v8 <c>nitro-board</c>
    /// value, or when it still carries the v9 <c>pid</c> column the v10
    /// schema drops, detected the same way
    /// <see cref="RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync"/>
    /// detects a stale <c>agent_sessions</c> table: by inspecting the
    /// table's own recorded SQL in <c>sqlite_master</c>, since SQLite has
    /// neither an ALTER for a CHECK constraint nor one that drops a primary
    /// key column. A no-op when the table is already current, or when it
    /// does not exist yet.
    /// </summary>
    /// <remarks>
    /// Follows the same drop-under-a-fresh-name-then-rename procedure as
    /// <see cref="RebuildAgentSessionsHarnessCheckConstraintIfStaleAsync"/>,
    /// but runs inside the caller's own transaction instead of a dedicated
    /// one with foreign keys disabled: unlike <c>agent_sessions</c>,
    /// <c>mail_wake_targets</c> is not the target of a foreign key from any
    /// other table, so dropping and recreating it never risks a cascading
    /// delete on a sibling table.
    /// </remarks>
    private static async Task RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'mail_wake_targets';",
            transaction: transaction);

        if (createTableSql is null
            || (createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
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
    /// <c>harness</c> CHECK constraint predates the v8 <c>nitro-board</c>
    /// value, or when it still carries the v9 <c>pid</c> column the v10
    /// schema drops: the same gaps and detection method
    /// <see cref="RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync"/>
    /// closes for <c>mail_wake_targets</c>. A no-op when the table is
    /// already current, or when it does not exist yet.
    /// </summary>
    /// <remarks>
    /// Follows the same procedure as
    /// <see cref="RebuildMailWakeTargetsHarnessCheckConstraintIfStaleAsync"/>,
    /// including running inside the caller's own transaction: like
    /// <c>mail_wake_targets</c>, <c>session_ping_gates</c> is not the target
    /// of a foreign key from any other table. Recreates
    /// <c>idx_session_ping_gates_expires</c> after the rename, since an
    /// index attached to the dropped table does not follow it.
    /// </remarks>
    private static async Task RebuildSessionPingGatesHarnessCheckConstraintIfStaleAsync(
        SqliteConnection connection,
        DbTransaction transaction)
    {
        var createTableSql = await connection.ExecuteScalarAsync<string?>(
            "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = 'session_ping_gates';",
            transaction: transaction);

        if (createTableSql is null
            || (createTableSql.Contains("'nitro-board'", StringComparison.Ordinal)
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

        var version = await connection.ExecuteScalarAsync<long>("PRAGMA user_version;");

        ValidateVersionForConnect(version);

        return connection;
    }

    /// <summary>
    /// Reads the schema version stamped on the workspace database at the
    /// given directory, without applying or validating anything against it.
    /// For callers that need to branch on whether an existing database is a
    /// plain-init upgrade candidate (see <see cref="IsUpgradableVersion"/>)
    /// before deciding what plain `agent init` should do, ahead of calling
    /// <see cref="InitializeAsync"/> itself.
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
        // Pooling would keep the database file open after the connection is
        // disposed; a CLI process runs one command and exits, so it gains
        // nothing from the pool.
        var connection = new SqliteConnection($"Data Source={databasePath};Pooling=False");

        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(
            "PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;");

        return connection;
    }
}

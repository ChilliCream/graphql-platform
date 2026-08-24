using System.Data.Common;
using Dapper;
using Microsoft.Data.Sqlite;
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
    public const int CurrentVersion = 4;

    /// <summary>
    /// Schema versions upgraded in place by <see cref="InitializeAsync"/>
    /// rather than rejected: v2 (before the agents table gained its role and
    /// implicit columns) and v3 (after those columns and the client column,
    /// before the v4 <see cref="AgentSessionSchema"/> tables). A v3
    /// database's agents table already carries every column
    /// <see cref="UpgradeAgentsTableAsync"/> adds, so upgrading it only means
    /// applying the new v4 tables and bumping the stamped version.
    /// </summary>
    private static readonly int[] UpgradableVersions = [2, 3];

    /// <summary>
    /// True for a schema version <see cref="InitializeAsync"/> upgrades in
    /// place instead of rejecting. Exposed so callers such as `agent init`
    /// can decide, before calling in, whether an existing database on disk
    /// is a plain-init upgrade candidate or requires <c>--force</c>.
    /// </summary>
    public static bool IsUpgradableVersion(long version) => Array.IndexOf(UpgradableVersions, (int)version) >= 0;

    static AgentDatabase() => SQLitePCL.Batteries_V2.Init();

    /// <summary>
    /// Opens a connection to a new or existing workspace database, rebuilds
    /// <c>agent_sessions</c>' <c>last_ping_result</c> CHECK constraint first
    /// if it predates <c>unsupported</c> (see
    /// <see cref="RebuildAgentSessionsCheckConstraintIfStaleAsync"/>, its own
    /// transaction since it needs foreign key enforcement off), then, in one
    /// further transaction, applies the task, mail, and agent registry
    /// schemas, upgrades the agents table's role, implicit, and client
    /// columns in place when any of them predate the database on hand, and
    /// stamps the current schema version. Returns the open connection so
    /// callers, including test seeding helpers, can write against it
    /// directly. Throws <see cref="ExitException"/> when the existing
    /// database's version is anything other than 0 (a genuinely new file),
    /// one of <see cref="UpgradableVersions"/>, or <see cref="CurrentVersion"/>,
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
        // and an upgradable v2 or v3 file (neither has an agent_sessions
        // table yet either) alike, without a version-keyed branch for a
        // change that never had its own version number.
        await RebuildAgentSessionsCheckConstraintIfStaleAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(TaskStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentRegistrySchema.Create, transaction: transaction);
        await connection.ExecuteAsync(MailStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentSessionSchema.Create, transaction: transaction);

        // Column-by-column, not gated on version == UpgradableVersion: the
        // client column shipped after CurrentVersion was last bumped to 3,
        // so an existing v3 database (this repo's own workspace included)
        // needs the same ALTER a v2 database does. Running it unconditionally,
        // guarded per column, covers a brand new file (CREATE TABLE already
        // has every column, so every check is a no-op), an upgradable v2
        // file, and a v3 file that predates client alike, with one code
        // path instead of a version-keyed branch per column ever added.
        await UpgradeAgentsTableAsync(connection, transaction);

        await connection.ExecuteAsync(
            $"""PRAGMA user_version = {CurrentVersion};""", transaction: transaction);

        await transaction.CommitAsync(cancellationToken);

        return connection;
    }

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
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
                    cwd, workspace_path, endpoint_kind, endpoint_addr, started_at, last_beat_at,
                    block_budget_used, last_ping_at, last_ping_attempt, last_ping_result, last_ping_detail
                )
                SELECT
                    harness, session_id, agent_name, binding_kind, host, pid, proc_start,
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
                """
                CREATE INDEX IF NOT EXISTS idx_agent_sessions_name ON agent_sessions (agent_name);
                CREATE INDEX IF NOT EXISTS idx_agent_sessions_pid ON agent_sessions (host, pid);
                """,
                transaction: transaction);

            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await connection.ExecuteAsync("PRAGMA foreign_keys = ON;");
        }
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
                + "workspace does not accept here. A legacy task or mail database is migrated "
                + "by `nitro agent init`, not opened directly.");
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
            throw new ExitException(
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

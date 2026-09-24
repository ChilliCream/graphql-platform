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
    public const int CurrentVersion = 18;

    /// <summary>
    /// The schema version at which <c>messages.sender</c> and
    /// <c>message_recipients.recipient</c> stopped referencing <c>agents (name)</c>
    /// as a foreign key. A database below this version has its mail tables
    /// rebuilt without that constraint before the rest of the upgrade runs.
    /// </summary>
    private const int MailForeignKeysRemovedVersion = 16;

    /// <summary>
    /// Schema versions <see cref="InitializeAsync"/> upgrades in place
    /// instead of rejecting.
    /// </summary>
    private static readonly int[] s_upgradableVersions = [2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17];

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

        if (version > 0 && version < MailForeignKeysRemovedVersion)
        {
            await RemoveMailAgentForeignKeysAsync(connection, cancellationToken);
        }

        if (version < CurrentVersion)
        {
            await ResetAgentDomainTablesAsync(connection, cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(TaskStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentRegistrySchema.Create, transaction: transaction);
        await connection.ExecuteAsync(MailStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(PingLeaseSchema.Create, transaction: transaction);

        await connection.ExecuteAsync(MailWakeSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentDeliverySchema.Create, transaction: transaction);
        await connection.ExecuteAsync(AgentPingGateSchema.Create, transaction: transaction);

        await connection.ExecuteAsync(MemoryStoreSchema.Create, transaction: transaction);

        await connection.ExecuteAsync(TakeoverLedgerSchema.Create, transaction: transaction);

        // Imports legacy markdown entries whose ids are absent from the database.
        await MemoryMarkdownImport.ImportAsync(
            connection, transaction, workspaceDirectory, cancellationToken);

        await connection.ExecuteAsync(
            $"""PRAGMA user_version = {CurrentVersion};""", transaction: transaction);

        await transaction.CommitAsync(cancellationToken);

        return connection;
    }

    /// <summary>
    /// Drops every agent-domain table (identity, session, wake, and ping
    /// state) so the schema creation that follows lays them down fresh. Mail,
    /// task, and memory tables are left untouched. A no-op for tables that do
    /// not exist yet.
    /// </summary>
    private static async Task ResetAgentDomainTablesAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await connection.ExecuteAsync(
                """
                DROP TABLE IF EXISTS agent_sessions;
                DROP TABLE IF EXISTS agent_session_identities;
                DROP TABLE IF EXISTS session_deliveries;
                DROP TABLE IF EXISTS session_ping_gates;
                DROP TABLE IF EXISTS ping_leases;
                DROP TABLE IF EXISTS mail_wake_outbox;
                DROP TABLE IF EXISTS mail_wake_batches;
                DROP TABLE IF EXISTS mail_wake_targets;
                DROP TABLE IF EXISTS mail_wake_daemons;
                DROP TABLE IF EXISTS agent_deliveries;
                DROP TABLE IF EXISTS agent_ping_gates;
                DROP TABLE IF EXISTS agents;
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
    /// Rebuilds <c>messages</c> and <c>message_recipients</c> so neither
    /// column carries a foreign key to <c>agents</c>, preserving every row.
    /// A no-op when the mail tables do not exist yet.
    /// </summary>
    private static async Task RemoveMailAgentForeignKeysAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var mailTablesExist = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'messages';") > 0;

        if (!mailTablesExist)
        {
            return;
        }

        await connection.ExecuteAsync("PRAGMA foreign_keys = OFF;");

        try
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await connection.ExecuteAsync(
                """
                CREATE TABLE messages_new (
                    id TEXT PRIMARY KEY,
                    thread_id TEXT NOT NULL,
                    in_reply_to TEXT REFERENCES messages (id),
                    sender TEXT NOT NULL,
                    subject TEXT NOT NULL CHECK (length(subject) BETWEEN 1 AND 500),
                    body TEXT NOT NULL,
                    created_at TEXT NOT NULL
                );

                INSERT INTO messages_new (id, thread_id, in_reply_to, sender, subject, body, created_at)
                SELECT id, thread_id, in_reply_to, sender, subject, body, created_at FROM messages;

                CREATE TABLE message_recipients_new (
                    message_id TEXT NOT NULL REFERENCES messages (id) ON DELETE CASCADE,
                    recipient TEXT NOT NULL,
                    kind TEXT NOT NULL DEFAULT 'to' CHECK (kind IN ('to', 'cc')),
                    ordinal INTEGER NOT NULL CHECK (ordinal >= 0),
                    read_at TEXT,
                    archived_at TEXT,
                    PRIMARY KEY (message_id, recipient),
                    UNIQUE (message_id, ordinal)
                );

                INSERT INTO message_recipients_new (message_id, recipient, kind, ordinal, read_at, archived_at)
                SELECT message_id, recipient, kind, ordinal, read_at, archived_at FROM message_recipients;

                DROP TABLE message_recipients;
                DROP TABLE messages;

                ALTER TABLE messages_new RENAME TO messages;
                ALTER TABLE message_recipients_new RENAME TO message_recipients;

                CREATE INDEX idx_messages_thread_id ON messages (thread_id);
                CREATE INDEX idx_messages_created_at ON messages (created_at);
                CREATE INDEX idx_messages_sender ON messages (sender);

                CREATE INDEX idx_message_recipients_recipient
                    ON message_recipients (recipient);
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

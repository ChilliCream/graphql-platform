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
    public const int CurrentVersion = 2;

    static AgentDatabase() => SQLitePCL.Batteries_V2.Init();

    /// <summary>
    /// Opens a connection to a new or existing workspace database, applies
    /// the task and mail schemas, and stamps the current schema version, all
    /// in one transaction. Returns the open connection so callers, including
    /// test seeding helpers, can write against it directly. Throws
    /// <see cref="ExitException"/> when the existing database's version is
    /// anything other than 0 (a genuinely new file) or
    /// <see cref="CurrentVersion"/>.
    /// </summary>
    public async Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken)
    {
        var connection = await OpenAsync(
            AgentWorkspace.GetDatabasePath(workspaceDirectory),
            cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var version = await connection.ExecuteScalarAsync<long>(
            "PRAGMA user_version;", transaction: transaction);

        ValidateVersionForInitialize(version);

        await connection.ExecuteAsync(TaskStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(MailStoreSchema.Create, transaction: transaction);
        await connection.ExecuteAsync(
            $"""PRAGMA user_version = {CurrentVersion};""", transaction: transaction);

        await transaction.CommitAsync(cancellationToken);

        return connection;
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

    private static void ValidateVersionForInitialize(long version)
    {
        if (version > CurrentVersion)
        {
            throw new ExitException(
                "The agent workspace was created by a newer version of the Nitro CLI "
                + $"(schema v{version}, supported up to v{CurrentVersion}). "
                + "Update the CLI to use it.");
        }

        if (version != 0 && version != CurrentVersion)
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

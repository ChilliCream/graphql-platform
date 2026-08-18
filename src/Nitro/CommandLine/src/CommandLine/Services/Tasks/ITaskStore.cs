using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// Opens task workspace databases and provides the shared operations that
/// task commands build on. Command-specific queries run directly against the
/// returned connections. A null transaction means the operation auto-commits.
/// </summary>
internal interface ITaskStore
{
    /// <summary>
    /// Creates or reopens the workspace database in the given workspace
    /// directory and applies the schema.
    /// </summary>
    Task<SqliteConnection> InitializeAsync(
        string workspaceDirectory,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens the nearest workspace found at or above the current directory.
    /// Throws <see cref="ExitException"/> when no workspace exists.
    /// </summary>
    Task<SqliteConnection> ConnectAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Returns the nearest workspace directory at or above the current
    /// directory, or null when no workspace exists.
    /// </summary>
    string? FindWorkspaceDirectory();

    Task<string?> GetConfigAsync(
        SqliteConnection connection,
        string key,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    Task SetConfigAsync(
        SqliteConnection connection,
        string key,
        string value,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Returns the workspace's task ID prefix.
    /// </summary>
    Task<string> GetPrefixAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Allocates a new unique task ID. With a parent ID, allocates the next
    /// hierarchical child ID (for example "app-1a2.3").
    /// </summary>
    Task<string> CreateTaskIdAsync(
        SqliteConnection connection,
        string? parentId,
        string seed,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Appends an entry to the task audit log.
    /// </summary>
    Task RecordEventAsync(
        SqliteConnection connection,
        TaskEvent taskEvent,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Returns the task with the given ID, or null. Tombstones are returned;
    /// callers decide whether they count.
    /// </summary>
    Task<TaskItem?> GetTaskAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Returns the task with the given ID or throws <see cref="ExitException"/>
    /// when it does not exist or is a tombstone.
    /// </summary>
    Task<TaskItem> GetRequiredTaskAsync(
        SqliteConnection connection,
        string id,
        CancellationToken cancellationToken,
        DbTransaction? transaction = null);

    /// <summary>
    /// Computes the set of blocked tasks from the dependency graph. Maps a
    /// blocked task ID to its blocker descriptions ("id:reason").
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> ComputeBlockedAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken);
}

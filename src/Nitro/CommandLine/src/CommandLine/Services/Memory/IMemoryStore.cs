using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

internal interface IMemoryStore
{
    /// <summary>
    /// Finds the nearest project agent workspace directory at or above the
    /// current directory that has a project memory store, per
    /// <see cref="AgentWorkspace.FindMemory"/>. Returns null when none
    /// exists.
    /// </summary>
    string? FindProjectWorkspaceDirectory();

    /// <summary>
    /// Creates the curated, journal, and local index directories of the
    /// project memory store under the given agent workspace directory.
    /// Idempotent: directories that already exist are left as they are.
    /// </summary>
    Task EnsureProjectWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new curated memory in the project store: normalizes and
    /// validates the type and tags, allocates an id, and writes the
    /// markdown file with atomic create-without-overwrite. Throws
    /// <see cref="ExitException"/> when no project workspace is found, or
    /// when the type or a tag is invalid.
    /// </summary>
    Task<MemoryRecord> SaveAsync(MemoryRecordCreation creation, CancellationToken cancellationToken);

    /// <summary>
    /// Updates one or more fields of an existing curated memory and
    /// atomically replaces its markdown file. Adding a tag that is already
    /// present, or removing one that is not, is a no-op for that tag.
    /// Throws <see cref="ExitException"/> when the memory does not exist, or
    /// when a given type or tag is invalid.
    /// </summary>
    Task<MemoryRecord> UpdateAsync(string id, MemoryRecordUpdate update, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes a curated memory's markdown file (hard delete, no
    /// tombstone) and returns the record as it was before deletion. Throws
    /// <see cref="ExitException"/> when the memory does not exist.
    /// </summary>
    Task<MemoryRecord> ForgetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id, or null when it does
    /// not exist or no project workspace is found. Throws
    /// <see cref="ExitException"/> when the file exists but its frontmatter
    /// fails to parse.
    /// </summary>
    Task<MemoryRecord?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id, or throws
    /// <see cref="ExitException"/> when it does not exist.
    /// </summary>
    Task<MemoryRecord> GetRequiredAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns curated memories ordered by <c>updated_at</c> descending,
    /// then id, up to the given limit (unlimited when null). Returns an
    /// empty list when no project workspace or no curated store exists yet.
    /// A file whose frontmatter fails to parse is skipped rather than
    /// failing the whole listing.
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(int? limit, CancellationToken cancellationToken);
}

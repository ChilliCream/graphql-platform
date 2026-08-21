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
    /// The resolved global memory root directory. Always available: unlike
    /// the project store, it does not depend on discovering a workspace,
    /// and its curated, journal, and local index directories are created
    /// lazily on first write.
    /// </summary>
    string GlobalMemoryDirectory { get; }

    /// <summary>
    /// Creates the curated, journal, and local index directories of the
    /// project memory store under the given agent workspace directory.
    /// Idempotent: directories that already exist are left as they are.
    /// </summary>
    Task EnsureProjectWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a new curated memory in the scope named by
    /// <see cref="MemoryRecordCreation.Scope"/>: normalizes and validates
    /// the scope, type, and tags, allocates an id, and writes the markdown
    /// file with atomic create-without-overwrite. Throws
    /// <see cref="ExitException"/> when the scope is project and no project
    /// workspace is found, or when the scope, type, or a tag is invalid.
    /// The global scope never fails this way: its directories are created
    /// lazily.
    /// </summary>
    Task<MemoryRecord> SaveAsync(MemoryRecordCreation creation, CancellationToken cancellationToken);

    /// <summary>
    /// Updates one or more fields of an existing curated memory in the
    /// given scope and atomically replaces its markdown file. Adding a tag
    /// that is already present, or removing one that is not, is a no-op
    /// for that tag. Throws <see cref="ExitException"/> when the scope is
    /// project and no project workspace is found, when the memory does not
    /// exist in that scope, or when a given type or tag is invalid.
    /// </summary>
    Task<MemoryRecord> UpdateAsync(
        string id, string scope, MemoryRecordUpdate update, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes a curated memory's markdown file in the given
    /// scope (hard delete, no tombstone) and returns the record as it was
    /// before deletion. Throws <see cref="ExitException"/> when the scope
    /// is project and no project workspace is found, or when the memory
    /// does not exist in that scope.
    /// </summary>
    Task<MemoryRecord> ForgetAsync(string id, string scope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id in the given scope, or
    /// null when it does not exist there. When scope is <see cref="MemoryScopes.All"/>,
    /// both stores are searched and the result is the union with no
    /// shadowing; a project workspace that cannot be found simply
    /// contributes nothing. When scope is <see cref="MemoryScopes.Project"/>
    /// and no project workspace is found, throws <see cref="ExitException"/>
    /// (the same error <see cref="SaveAsync"/> gives) rather than reporting
    /// a missing record. Throws <see cref="ExitException"/> when a matching
    /// file's frontmatter fails to parse, and
    /// <see cref="MemoryScopeConflictException"/> when scope is
    /// <see cref="MemoryScopes.All"/> and the id exists in both stores.
    /// </summary>
    Task<MemoryRecord?> FindAsync(string id, string scope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id in the given scope, or
    /// throws <see cref="ExitException"/> when it does not exist there. See
    /// <see cref="FindAsync"/> for the full scope-resolution and failure
    /// contract.
    /// </summary>
    Task<MemoryRecord> GetRequiredAsync(string id, string scope, CancellationToken cancellationToken);

    /// <summary>
    /// Returns curated memories in the given scope ordered project band
    /// first, then global; within each band by <c>updated_at</c>
    /// descending, then id; up to the given limit across both bands
    /// combined (unlimited when null). Returns an empty list when a
    /// requested store has no project workspace, or no curated directory
    /// yet. A file whose frontmatter fails to parse throws
    /// <see cref="ExitException"/> rather than being skipped, and scope
    /// <see cref="MemoryScopes.All"/> throws
    /// <see cref="MemoryScopeConflictException"/> when the same id exists
    /// in both stores.
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(
        string scope, int? limit, CancellationToken cancellationToken);
}

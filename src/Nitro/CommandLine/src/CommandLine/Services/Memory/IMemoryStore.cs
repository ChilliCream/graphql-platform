namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Curated memories and the journal, stored in the workspace database
/// beside tasks and mail. There is one store per workspace and no scope:
/// a memory belongs to the workspace it was written in, the same way a task
/// does. Every method throws <see cref="ExitException"/> when no agent
/// workspace resolves from the current directory.
/// </summary>
internal interface IMemoryStore
{
    /// <summary>
    /// The agent workspace directory memory reads and writes resolve to, or
    /// null when the current directory is not inside one.
    /// </summary>
    string? FindWorkspaceDirectory();

    /// <summary>
    /// Saves a new curated memory: validates the type, tags, and actor,
    /// allocates an id, and inserts it. Throws <see cref="ExitException"/>
    /// when the type, a tag, or the actor is invalid.
    /// </summary>
    Task<MemoryRecord> SaveAsync(MemoryRecordCreation creation, CancellationToken cancellationToken);

    /// <summary>
    /// Updates one or more fields of an existing curated memory. Adding a
    /// tag that is already present, or removing one that is not, is a no-op
    /// for that tag. Throws <see cref="ExitException"/> when the memory does
    /// not exist, or when a given type or tag is invalid.
    /// </summary>
    Task<MemoryRecord> UpdateAsync(string id, MemoryRecordUpdate update, CancellationToken cancellationToken);

    /// <summary>
    /// Permanently deletes a curated memory (hard delete, no tombstone) and
    /// returns the record as it was before deletion. Throws
    /// <see cref="ExitException"/> when the memory does not exist.
    /// </summary>
    Task<MemoryRecord> ForgetAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id, or null when it does
    /// not exist.
    /// </summary>
    Task<MemoryRecord?> FindAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memory with the given id, or throws
    /// <see cref="ExitException"/> when it does not exist.
    /// </summary>
    Task<MemoryRecord> GetRequiredAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns curated memories by <c>updated_at</c> descending, then id, up
    /// to the given limit (unlimited when null).
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(int? limit, CancellationToken cancellationToken);

    /// <summary>
    /// Searches curated memories by literal lexical match against
    /// <paramref name="query"/> (never interpreted as FTS5 query syntax),
    /// narrowed by tags (AND), type, and a minimum updated-at timestamp.
    /// Ordered by FTS rank, then <c>updated_at</c> descending, then id, up
    /// to the given limit (unlimited when null).
    /// </summary>
    Task<IReadOnlyList<MemoryRecord>> SearchCuratedAsync(
        string query,
        IReadOnlyList<string> tags,
        string? type,
        DateTimeOffset? since,
        int? limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Captures a new journal entry: allocates an id and inserts it. Throws
    /// <see cref="ExitException"/> when the actor is invalid.
    /// </summary>
    Task<MemoryJournalEntry> LogAsync(MemoryJournalEntryCreation creation, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the journal entry with the given id, or null when it does not
    /// exist.
    /// </summary>
    Task<MemoryJournalEntry?> FindJournalEntryAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the journal entry with the given id, or throws
    /// <see cref="ExitException"/> when it does not exist.
    /// </summary>
    Task<MemoryJournalEntry> GetRequiredJournalEntryAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns journal entries by <c>created_at</c> descending, then id, up
    /// to the given limit (unlimited when null).
    /// </summary>
    Task<IReadOnlyList<MemoryJournalEntry>> GetRecentJournalAsync(int? limit, CancellationToken cancellationToken);

    /// <summary>
    /// Searches journal entries by literal, case insensitive substring match
    /// of every whitespace-separated word in <paramref name="query"/>
    /// against the entry body, narrowed by a minimum created-at timestamp. A
    /// journal entry has no type or tags to filter by.
    /// </summary>
    Task<IReadOnlyList<MemoryJournalEntry>> SearchJournalAsync(
        string query, DateTimeOffset? since, int? limit, CancellationToken cancellationToken);

    /// <summary>
    /// Returns journal entries that have not yet been promoted, ordered the
    /// same way as <see cref="GetRecentJournalAsync"/>.
    /// </summary>
    Task<IReadOnlyList<MemoryJournalEntry>> GetUnpromotedJournalEntriesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Mechanically copies the journal entry with the given id into a new
    /// curated memory. The curated id is derived deterministically from the
    /// journal id and <c>promoted_from</c> is unique, so promoting the same
    /// entry again, including concurrently, is idempotent: the existing
    /// curated memory is returned with
    /// <see cref="MemoryPromotionOutcome.AlreadyPromoted"/> true instead of
    /// failing or duplicating. Throws <see cref="ExitException"/> when the
    /// journal entry does not exist, or when the given type or a tag is
    /// invalid.
    /// </summary>
    Task<MemoryPromotionOutcome> PromoteAsync(
        string journalId,
        string type,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken);
}

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Stores curated memories and journal entries in the current agent workspace.
/// Storage operations throw <see cref="ExitException"/> when no workspace resolves;
/// <see cref="FindWorkspaceDirectory"/> returns null instead.
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
    /// Updates the supplied fields and refreshes the memory's modification time,
    /// removing tags before adding tags. Throws <see cref="ExitException"/> for a
    /// missing memory or invalid type or tag; duplicate additions and absent removals
    /// do not change the tag set.
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
    /// Copies a journal entry into a curated memory, or returns its existing promotion
    /// with <see cref="MemoryPromotionOutcome.AlreadyPromoted"/> set.
    /// Throws <see cref="ExitException"/> for a missing journal entry or invalid type or tag.
    /// </summary>
    Task<MemoryPromotionOutcome> PromoteAsync(
        string journalId,
        string type,
        IReadOnlyList<string> tags,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the curated memories and journal entries the given agent created,
    /// a promoted journal entry appearing once, as the curated memory. Ordered by
    /// <c>created_at</c> descending, then id, up to the given limit (unlimited when
    /// null).
    /// </summary>
    Task<IReadOnlyList<MemoryParticipationEntry>> QueryParticipationAsync(
        string agent, int? limit, CancellationToken cancellationToken);
}

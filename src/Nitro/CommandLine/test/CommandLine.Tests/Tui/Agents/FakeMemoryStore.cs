using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Agents;

/// <summary>
/// An <see cref="IMemoryStore"/> exposing <see cref="QueryParticipationAsync"/>,
/// <see cref="GetRequiredAsync"/>, and <see cref="GetRequiredJournalEntryAsync"/> with
/// configurable rows, for tests of the agent detail popover's Memory section and its
/// drilled-into detail view. Every other member throws <see cref="NotSupportedException"/>.
/// </summary>
internal sealed class FakeMemoryStore : IMemoryStore
{
    /// <summary>
    /// The rows <see cref="QueryParticipationAsync"/> returns, sliced to the requested
    /// limit; empty by default.
    /// </summary>
    public IReadOnlyList<MemoryParticipationEntry> ParticipationRows { get; set; } = [];

    /// <summary>
    /// The curated memories <see cref="GetRequiredAsync"/> returns, keyed by id; empty by
    /// default.
    /// </summary>
    public Dictionary<string, MemoryRecord> CuratedRecords { get; } = [];

    /// <summary>
    /// The journal entries <see cref="GetRequiredJournalEntryAsync"/> returns, keyed by id;
    /// empty by default.
    /// </summary>
    public Dictionary<string, MemoryJournalEntry> JournalEntries { get; } = [];

    public Task<IReadOnlyList<MemoryParticipationEntry>> QueryParticipationAsync(
        string agent, int? limit, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyList<MemoryParticipationEntry>>(
            limit is { } max ? [.. ParticipationRows.Take(max)] : ParticipationRows);

    public string? FindWorkspaceDirectory() => throw new NotSupportedException();

    public Task<MemoryRecord> SaveAsync(MemoryRecordCreation creation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryRecord> UpdateAsync(string id, MemoryRecordUpdate update, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryRecord> ForgetAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryRecord?> FindAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryRecord> GetRequiredAsync(string id, CancellationToken cancellationToken)
        => CuratedRecords.TryGetValue(id, out var record)
            ? Task.FromResult(record)
            : throw new ExitException($"Memory '{id}' not found.");

    public Task<IReadOnlyList<MemoryRecord>> GetRecentCuratedAsync(int? limit, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MemoryRecord>> SearchCuratedAsync(
        string query,
        IReadOnlyList<string> tags,
        string? type,
        DateTimeOffset? since,
        int? limit,
        CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryJournalEntry> LogAsync(MemoryJournalEntryCreation creation, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryJournalEntry?> FindJournalEntryAsync(string id, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryJournalEntry> GetRequiredJournalEntryAsync(string id, CancellationToken cancellationToken)
        => JournalEntries.TryGetValue(id, out var entry)
            ? Task.FromResult(entry)
            : throw new ExitException($"Journal entry '{id}' not found.");

    public Task<IReadOnlyList<MemoryJournalEntry>> GetRecentJournalAsync(int? limit, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MemoryJournalEntry>> SearchJournalAsync(
        string query, DateTimeOffset? since, int? limit, CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<MemoryJournalEntry>> GetUnpromotedJournalEntriesAsync(CancellationToken cancellationToken)
        => throw new NotSupportedException();

    public Task<MemoryPromotionOutcome> PromoteAsync(
        string journalId, string type, IReadOnlyList<string> tags, CancellationToken cancellationToken)
        => throw new NotSupportedException();
}

using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Loads up to 200 curated memories or up to 200 journal entries for the parsed query, for
/// <see cref="MemoryState"/> to merge into one table. Type and tag filters apply only to
/// curated memories.
/// </summary>
internal sealed class MemoryDataLoader(IMemoryStore store)
{
    private const int Limit = 200;

    public async Task<IReadOnlyList<MemoryRecord>> LoadCuratedAsync(
        MemoryQuery query, CancellationToken cancellationToken)
    {
        if (query.Text.Length > 0)
        {
            return await store.SearchCuratedAsync(
                query.Text, query.Tags, query.Type, since: null, Limit, cancellationToken)
                .ConfigureAwait(false);
        }

        if (query.Type is null && query.Tags.Count == 0)
        {
            return await store.GetRecentCuratedAsync(Limit, cancellationToken).ConfigureAwait(false);
        }

        var recent = await store.GetRecentCuratedAsync(int.MaxValue, cancellationToken).ConfigureAwait(false);

        return FilterCurated(recent, query).Take(Limit).ToList();
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> LoadJournalAsync(
        MemoryQuery query, CancellationToken cancellationToken)
        => query.Text.Length > 0
            ? await store.SearchJournalAsync(query.Text, since: null, Limit, cancellationToken)
                .ConfigureAwait(false)
            : await store.GetRecentJournalAsync(Limit, cancellationToken).ConfigureAwait(false);

    private static IReadOnlyList<MemoryRecord> FilterCurated(IReadOnlyList<MemoryRecord> records, MemoryQuery query)
    {
        IEnumerable<MemoryRecord> filtered = records;

        if (query.Type is { } type)
        {
            filtered = filtered.Where(r => r.Type == type);
        }

        foreach (var tag in query.Tags)
        {
            var normalizedTag = tag;
            filtered = filtered.Where(r => r.Tags.Contains(normalizedTag));
        }

        return filtered.ToList();
    }
}

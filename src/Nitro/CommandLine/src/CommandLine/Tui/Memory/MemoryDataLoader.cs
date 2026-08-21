using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Tui.Memory;

/// <summary>
/// Loads the memory tab's curated and journal lists from the memory store,
/// through the same reads the CLI's <c>recent</c> and <c>search</c> commands
/// use: <see cref="IMemoryStore.SearchCuratedAsync"/>/<see cref="IMemoryStore.SearchJournalAsync"/>
/// once a <see cref="MemoryQuery"/> carries free text, or
/// <see cref="IMemoryStore.GetRecentCuratedAsync"/>/<see cref="IMemoryStore.GetRecentJournalAsync"/>
/// otherwise, narrowed client side by a <see cref="MemoryQuery"/>'s type and
/// tags when it has no free text (an empty FTS5 literal match is not a
/// query the store's search API is asked to answer): the recent read pulls
/// every candidate row unbounded, filters, then applies the limit, so a
/// type or tag filter narrows the returned page instead of narrowing an
/// already-limited page. Issues no SQL of its own.
/// </summary>
internal sealed class MemoryDataLoader(IMemoryStore store)
{
    private const int Limit = 200;

    public async Task<IReadOnlyList<MemoryRecord>> LoadCuratedAsync(
        string scope, MemoryQuery query, CancellationToken cancellationToken)
    {
        if (query.Text.Length > 0)
        {
            return await store.SearchCuratedAsync(
                query.Text, scope, query.Tags, query.Type, since: null, Limit, cancellationToken)
                .ConfigureAwait(false);
        }

        if (query.Type is null && query.Tags.Count == 0)
        {
            return await store.GetRecentCuratedAsync(scope, Limit, cancellationToken).ConfigureAwait(false);
        }

        var recent = await store.GetRecentCuratedAsync(scope, int.MaxValue, cancellationToken).ConfigureAwait(false);

        return FilterCurated(recent, query).Take(Limit).ToList();
    }

    public async Task<IReadOnlyList<MemoryJournalEntry>> LoadJournalAsync(
        string scope, MemoryQuery query, CancellationToken cancellationToken)
        => query.Text.Length > 0
            ? await store.SearchJournalAsync(query.Text, scope, since: null, Limit, cancellationToken)
                .ConfigureAwait(false)
            : await store.GetRecentJournalAsync(scope, Limit, cancellationToken).ConfigureAwait(false);

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

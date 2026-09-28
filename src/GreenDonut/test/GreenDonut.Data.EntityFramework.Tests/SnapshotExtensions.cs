namespace GreenDonut.Data;

public static class SnapshotExtensions
{
    /// <summary>
    /// Drains a <see cref="StreamPage{T}"/> and adds its items, cursors, and page info to the
    /// snapshot in the same shape a <see cref="Page{T}"/> snapshot would carry.
    /// </summary>
    public static async Task<Snapshot> AddStreamPageAsync<T>(
        this Snapshot snapshot,
        StreamPage<T> page,
        CancellationToken cancellationToken)
    {
        List<PageEntry<T>> entries = [];

        await foreach (var entry in page.EnumerateEntriesAsync(cancellationToken))
        {
            entries.Add(entry);
        }

        snapshot.Add(
            new
            {
                Index = page.Index,
                TotalCount = await page.TotalCountAsync(cancellationToken),
                HasNextPage = await page.HasNextPageAsync(cancellationToken),
                HasPreviousPage = await page.HasPreviousPageAsync(cancellationToken),
                Items = entries.ConvertAll(e => e.Item),
                Cursors = entries.ConvertAll(page.CreateCursor)
            });

        return snapshot;
    }

    /// <summary>
    /// Drains a <see cref="StreamPage{T}"/> and matches a markdown snapshot of its items, cursors
    /// and page info, the streaming twin of calling <c>MatchMarkdownSnapshot()</c> on a
    /// <see cref="Page{T}"/>.
    /// </summary>
    public static async Task MatchMarkdownSnapshotAsync<T>(
        this StreamPage<T> page,
        CancellationToken cancellationToken,
        object? postFix = null)
    {
        var snapshot = Snapshot.Create(postFix?.ToString());
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    public static Snapshot AddQueries(
        this Snapshot snapshot,
        List<QueryInfo> queries)
    {
        for (var i = 0; i < queries.Count; i++)
        {
            snapshot
                .Add(queries[i].QueryText, $"SQL {i}", "sql")
                .Add(queries[i].ExpressionText, $"Expression {i}");
        }

        return snapshot;
    }

    public static Snapshot AddSql(
        this Snapshot snapshot,
        CapturePagingQueryInterceptor interceptor)
    {
        for (var i = 0; i < interceptor.Queries.Count; i++)
        {
            snapshot.Add(interceptor.Queries[i].QueryText, $"SQL {i}", "sql");
        }

        return snapshot;
    }
}

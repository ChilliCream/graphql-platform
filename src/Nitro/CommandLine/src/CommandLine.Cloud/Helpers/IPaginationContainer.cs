namespace ChilliCream.Nitro.CommandLine.Cloud;

internal interface IPaginationContainer<TEdge>
{
    IPaginationContainer<TEdge> PageSize(int size);

    bool HasNext();

    bool HasPrevious();

    ValueTask<IReadOnlyList<TEdge>> GetCurrentAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TEdge>> FetchNextAsync(CancellationToken cancellationToken);

    ValueTask<IReadOnlyList<TEdge>> FetchPreviousAsync(CancellationToken cancellationToken);
}

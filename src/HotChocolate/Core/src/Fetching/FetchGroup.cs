namespace HotChocolate.Fetching;

public delegate Task<ILookup<TKey, TValue>> FetchGroup<TKey, TValue>(
    IReadOnlyList<TKey> keys,
    CancellationToken cancellationToken)
    where TKey : notnull;

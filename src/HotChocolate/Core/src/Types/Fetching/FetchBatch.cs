namespace HotChocolate.Fetching;

public delegate Task<IReadOnlyDictionary<TKey, TValue>> FetchBatch<TKey, TValue>(
    IReadOnlyList<TKey> keys,
    CancellationToken cancellationToken)
    where TKey : notnull;

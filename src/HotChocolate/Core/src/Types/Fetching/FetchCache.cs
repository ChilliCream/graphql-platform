namespace HotChocolate.Fetching;

public delegate Task<TValue> FetchCache<in TKey, TValue>(
    TKey key,
    CancellationToken cancellationToken)
    where TKey : notnull;

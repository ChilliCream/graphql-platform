namespace GreenDonut;

/// <summary>
/// A data fetching delegate for <c>DataLoader</c>. For every provided key
/// must be a result returned. Also to be mentioned is, the results must be
/// returned in the exact same order the keys were provided.
/// </summary>
/// <typeparam name="TKey">A key type.</typeparam>
/// <typeparam name="TValue">A value type.</typeparam>
/// <param name="keys">A list of keys.</param>
/// <param name="results">
/// The resolved values which need to be in the exact same
/// order as the keys were provided.
/// </param>
/// <param name="cancellationToken">A cancellation token.</param>
/// <returns>
/// A list of results which are in the exact same order as the provided
/// keys.
/// </returns>
public delegate ValueTask FetchDataDelegate<in TKey, TValue>(
    IReadOnlyList<TKey> keys,
    Memory<Result<TValue?>> results,
    CancellationToken cancellationToken)
    where TKey : notnull;

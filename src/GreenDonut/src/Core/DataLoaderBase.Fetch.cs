using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenDonut;

public abstract partial class DataLoaderBase<TKey, TValue>
{
    /// <summary>
    /// A batch loading function which has to be implemented for each
    /// individual <c>DataLoader</c>. For every provided key must be a
    /// result returned. Also to be mentioned is, the results must be
    /// returned in the exact same order the keys were provided.
    /// </summary>
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
    protected abstract ValueTask FetchAsync(
        IReadOnlyList<TKey> keys,
        Memory<Result<TValue>> results,
        CancellationToken cancellationToken);
}
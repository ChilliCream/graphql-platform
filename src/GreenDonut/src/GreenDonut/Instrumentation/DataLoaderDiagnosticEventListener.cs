namespace GreenDonut;

/// <summary>
/// A base class to create a DataLoader diagnostic event listener.
/// </summary>
public class DataLoaderDiagnosticEventListener : IDataLoaderDiagnosticEventListener
{
    /// <summary>
    /// A no-op <see cref="IDisposable"/> that can be returned from
    /// event methods that are not interested in when the scope is disposed.
    /// </summary>
    protected static IDisposable EmptyScope { get; } = new EmptyActivityScope();

    /// <inheritdoc />
    public virtual void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task) { }

    /// <inheritdoc />
    public virtual IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
        where TKey : notnull
        => EmptyScope;

    /// <inheritdoc />
    public virtual void BatchResults<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        ReadOnlySpan<Result<TValue?>> values)
        where TKey : notnull { }

    /// <inheritdoc />
    public virtual void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
        where TKey : notnull { }

    /// <inheritdoc />
    public virtual void BatchItemError<TKey>(
        TKey key,
        Exception error)
        where TKey : notnull { }

    private sealed class EmptyActivityScope : IDisposable
    {
        public void Dispose() { }
    }
}

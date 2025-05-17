namespace GreenDonut;

internal class AggregateDataLoaderDiagnosticEventListener(
    IDataLoaderDiagnosticEventListener[] listeners)
    : DataLoaderDiagnosticEventListener
{
    public override void ResolvedTaskFromCache(
        IDataLoader dataLoader,
        PromiseCacheKey cacheKey,
        Task task)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].ResolvedTaskFromCache(dataLoader, cacheKey, task);
        }
    }

    public override IDisposable ExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys)
    {
        var scopes = new IDisposable[listeners.Length];

        for (var i = 0; i < listeners.Length; i++)
        {
            scopes[i] = listeners[i].ExecuteBatch(dataLoader, keys);
        }

        return new AggregateEventScope(scopes);
    }

    public override void BatchResults<TKey, TValue>(
        IReadOnlyList<TKey> keys,
        ReadOnlySpan<Result<TValue?>> values)
        where TValue : default
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].BatchResults(keys, values);
        }
    }

    public override void BatchError<TKey>(
        IReadOnlyList<TKey> keys,
        Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].BatchError(keys, error);
        }
    }

    public override void BatchItemError<TKey>(
        TKey key,
        Exception error)
    {
        for (var i = 0; i < listeners.Length; i++)
        {
            listeners[i].BatchItemError(key, error);
        }
    }

    private sealed class AggregateEventScope(IDisposable[] scopes) : IDisposable
    {
        public void Dispose()
        {
            for (var i = 0; i < scopes.Length; i++)
            {
                scopes[i].Dispose();
            }
        }
    }
}

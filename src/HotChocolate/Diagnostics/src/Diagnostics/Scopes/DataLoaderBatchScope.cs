using System.Diagnostics;
using GreenDonut;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class DataLoaderBatchScope<TKey> : IDisposable where TKey : notnull
{
    private readonly ActivityEnricher _enricher;
    private readonly IDataLoader _dataLoader;
    private readonly IReadOnlyList<TKey> _keys;
    private readonly Activity _activity;
    private bool _disposed;

    public DataLoaderBatchScope(
        ActivityEnricher enricher,
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys,
        Activity activity)
    {
        _enricher = enricher;
        _dataLoader = dataLoader;
        _keys = keys;
        _activity = activity;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _enricher.EnrichDataLoaderBatch(_dataLoader, _keys, _activity);
            _activity.Dispose();
            _disposed = true;
        }
    }
}

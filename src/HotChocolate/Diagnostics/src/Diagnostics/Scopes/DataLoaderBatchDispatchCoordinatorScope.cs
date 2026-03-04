using System.Diagnostics;

namespace HotChocolate.Diagnostics.Scopes;

internal sealed class DataLoaderBatchDispatchCoordinatorScope : IDisposable
{
    private readonly ActivityEnricher _enricher;
    private readonly Activity _activity;
    private bool _disposed;

    public DataLoaderBatchDispatchCoordinatorScope(
        ActivityEnricher enricher,
        Activity activity)
    {
        _enricher = enricher;
        _activity = activity;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _enricher.EnrichDataLoaderBatchDispatchCoordinator(_activity);
            _activity.Dispose();
            _disposed = true;
        }
    }
}

using System.Diagnostics;
using HotChocolate.Execution;

namespace HotChocolate.Diagnostics.Scopes;

internal class RequestScopeBase : IDisposable
{
    private bool _disposed;

    protected RequestScopeBase(
        ActivityEnricher enricher,
        IRequestContext context,
        Activity activity)
    {
        Enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    protected ActivityEnricher Enricher { get; }

    protected IRequestContext Context { get; }

    protected Activity Activity { get; }

    protected virtual void EnrichActivity() { }

    protected virtual void SetStatus() { }

    public void Dispose()
    {
        if (!_disposed)
        {
            EnrichActivity();
            SetStatus();
            Activity.Dispose();
            _disposed = true;
        }
    }
}

using System.Diagnostics;
using HotChocolate.Fusion.Execution;

namespace HotChocolate.Fusion.Diagnostics.Scopes;

internal class NodeScopeBase : IDisposable
{
    private bool _disposed;

    protected NodeScopeBase(
        FusionActivityEnricher enricher,
        OperationPlanContext context,
        Activity activity)
    {
        Enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        Context = context ?? throw new ArgumentNullException(nameof(context));
        Activity = activity ?? throw new ArgumentNullException(nameof(activity));
    }

    protected FusionActivityEnricher Enricher { get; }

    protected OperationPlanContext Context { get; }

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

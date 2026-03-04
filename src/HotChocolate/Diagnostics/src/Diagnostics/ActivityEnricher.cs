using System.Diagnostics;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Resolvers;

namespace HotChocolate.Diagnostics;

/// <summary>
/// The activity enricher allows adding additional information to the activity spans
/// created by the Hot Chocolate diagnostics system.
/// You can inherit from this class and override the enricher methods to add
/// additional information to the spans.
/// </summary>
public class ActivityEnricher(InstrumentationOptions options) : ActivityEnricherBase
{
    protected InstrumentationOptions Options { get; } = options;

    public virtual void EnrichCompileOperation(
        Activity activity,
        RequestContext context) { }

    public virtual void EnrichResolveFieldValue(
        Activity activity,
        IMiddlewareContext context) { }

    public virtual void EnrichResolverError(
        Activity activity,
        IMiddlewareContext context,
        IError error) { }

    public virtual void EnrichExecuteBatch<TKey>(
        Activity activity,
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys) where TKey : notnull { }

    public virtual void EnrichRunBatchDispatchCoordinator(
        Activity activity) { }

    public virtual void EnrichBatchDispatchError(
        Activity activity,
        Exception exception) { }

    public virtual void EnrichOnSubscriptionEvent(
        Activity activity,
        RequestContext context,
        ulong subscriptionId) { }
}

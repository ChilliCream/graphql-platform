using System.Diagnostics;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Language;
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
        RequestContext context,
        Activity activity)
    { }

    public virtual void EnrichResolveFieldValue(
        IMiddlewareContext context,
        Activity activity)
    { }

    public virtual void EnrichResolverError(
        IMiddlewareContext context,
        IError error,
        Activity activity)
    { }

    public virtual void EnrichExecuteBatch<TKey>(
        IDataLoader dataLoader,
        IReadOnlyList<TKey> keys,
        Activity activity) where TKey : notnull
    { }

    public virtual void EnrichRunBatchDispatchCoordinator(
        Activity activity)
    { }

    public virtual void EnrichBatchDispatchError(
        Exception exception,
        Activity activity)
    { }

    public virtual void EnrichDocumentNotFoundInStorage(
        RequestContext context,
        OperationDocumentId documentId,
        Activity activity)
    { }

    public virtual void EnrichUntrustedDocumentRejected(
        RequestContext context,
        Activity activity)
    { }

    public virtual void EnrichOnSubscriptionEvent(
        RequestContext context,
        ulong subscriptionId,
        Activity activity)
    { }
}

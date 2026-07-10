using System.Diagnostics;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class SubscriptionEventSpan(Activity activity, RequestContext context) : SpanBase(activity)
{
    public static SubscriptionEventSpan? Start(
        ActivitySource source,
        RequestContext context,
        string? operationName,
        ulong subscriptionId,
        ActivityContext? subscriptionContext = null)
    {
        var activity = subscriptionContext is { } parent
            ? source.StartActivity("GraphQL Subscription Event", ActivityKind.Internal, parent)
            : source.StartActivity("GraphQL Subscription Event");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.SubscriptionEvent);
        activity.EnrichOperation(OperationType.Subscription, operationName);
        activity.EnrichDocumentInfo(context.OperationDocumentInfo);
        activity.SetTag(GraphQL.Subscription.Id, subscriptionId.ToString(CultureInfo.InvariantCulture));

        return new SubscriptionEventSpan(activity, context);
    }

    protected override void OnComplete()
    {
        if (Activity.Status == ActivityStatusCode.Error)
        {
            return;
        }

        // If the caller dropped the connection while this event was in flight the
        // event was cancelled intentionally. Per the OpenTelemetry semantic
        // conventions that is not an error, so the span is left Unset instead of
        // being forced to Ok. A per-event timeout instead marks the span as Error
        // (handled in SubscriptionEventError) and is filtered out above.
        if (context.RequestAborted.IsCancellationRequested)
        {
            return;
        }

        Activity.SetStatus(ActivityStatusCode.Ok);
    }
}

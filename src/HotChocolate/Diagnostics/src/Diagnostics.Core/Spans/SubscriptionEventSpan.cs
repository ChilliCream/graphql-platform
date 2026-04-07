using System.Diagnostics;
using System.Globalization;
using HotChocolate.Execution;
using HotChocolate.Language;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class SubscriptionEventSpan(Activity activity) : SpanBase(activity)
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

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);
        activity.EnrichOperation(OperationType.Subscription, operationName);
        activity.EnrichDocumentInfo(context.OperationDocumentInfo);
        activity.SetTag(GraphQL.Subscription.Id, subscriptionId.ToString(CultureInfo.InvariantCulture));

        return new SubscriptionEventSpan(activity);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }
    }
}

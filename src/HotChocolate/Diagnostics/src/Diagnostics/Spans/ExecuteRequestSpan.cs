using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteRequestSpan(
    Activity activity,
    RequestContext context,
    InstrumentationOptionsBase options,
    ActivityEnricherBase? enricher,
    bool shouldDisposeActivity) : ExecuteRequestSpanBase(activity, context, options, enricher, shouldDisposeActivity)
{
    public static ExecuteRequestSpan? Start(
        ActivitySource source,
        RequestContext context,
        InstrumentationOptionsBase options,
        ActivityEnricherBase enricher)
    {
        var activity = StartActivity(source);

        if (activity is null)
        {
            return null;
        }

        return new ExecuteRequestSpan(
            activity,
            context,
            options,
            enricher,
            true);
    }

    protected override void OnComplete()
    {
        if (Context.TryGetOperation(out var operation))
        {
            var operationType = GraphQL.Operation.TypeValues[operation.Kind];
            Activity.SetTag(GraphQL.Operation.Type, operationType);
            Activity.DisplayName = operationType;

            var operationName = operation.Name;
            if (!string.IsNullOrEmpty(operationName))
            {
                Activity.SetTag(GraphQL.Operation.Name, operationName);
            }
        }

        base.OnComplete();
    }
}

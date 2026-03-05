using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Language;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class VariableCoercionSpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    public static VariableCoercionSpan? Start(
        ActivitySource source,
        RequestContext context,
        OperationType operationType,
        string? operationName,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Variable Coercion");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

        activity.EnrichOperation(operationType, operationName);
        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new VariableCoercionSpan(activity, context, enricher);
    }

    protected override void OnComplete()
    {
        if (context.VariableValues.Length > 0)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichCoerceVariables(context, Activity);
    }
}

using System.Diagnostics;
using HotChocolate.Execution;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class AnalyzeOperationComplexitySpan(
    Activity activity,
    RequestContext context,
    ActivityEnricherBase enricher) : SpanBase(activity)
{
    private bool _costSet;

    public static AnalyzeOperationComplexitySpan? Start(
        ActivitySource source,
        RequestContext context,
        ActivityEnricherBase enricher)
    {
        var activity = source.StartActivity("GraphQL Complexity Analyzation");

        if (activity is null)
        {
            return null;
        }

        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new AnalyzeOperationComplexitySpan(activity, context, enricher);
    }

    public void SetCost(double fieldCost, double typeCost)
    {
        Activity.SetTag(GraphQL.Operation.FieldCost, fieldCost);
        Activity.SetTag(GraphQL.Operation.TypeCost, typeCost);

        _costSet = true;

        enricher.EnrichOperationCost(context, fieldCost, typeCost, Activity);
    }

    protected override void OnComplete()
    {
        if (_costSet)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichAnalyzeOperationCost(context, Activity);
    }
}

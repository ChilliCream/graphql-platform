using System.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Diagnostics;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class EvaluateRequestPoliciesSpan(Activity activity) : SpanBase(activity)
{
    public static EvaluateRequestPoliciesSpan? Start(
        ActivitySource source,
        RequestContext context)
    {
        var activity = context.Features.TryGet<ExecuteRequestSpan>(out var requestSpan)
            ? source.StartActivity(
                "GraphQL Policy Evaluation",
                ActivityKind.Internal,
                requestSpan.Activity.Context)
            : source.StartActivity(
                "GraphQL Policy Evaluation",
                ActivityKind.Internal);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.PolicyEvaluate);

        if (context.GetOperationPlan() is { } plan)
        {
            activity.EnrichOperation(plan.Operation.Definition.Operation, plan.OperationName);
        }

        activity.EnrichDocumentInfo(context.OperationDocumentInfo);

        return new EvaluateRequestPoliciesSpan(activity);
    }

    protected override void OnComplete()
        => Activity.SetStatus(ActivityStatusCode.Ok);
}

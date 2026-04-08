using System.Diagnostics;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecuteSourceSchemaRequestSpan(
    Activity activity,
    OperationPlanContext context,
    OperationExecutionNode node,
    string schemaName,
    FusionActivityEnricher enricher) : SpanBase(activity)
{
    public static ExecuteSourceSchemaRequestSpan? Start(
        ActivitySource source,
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName,
        FusionActivityEnricher enricher)
    {
        var operation = node.Operation;
        var operationType = GraphQL.Operation.TypeValues[operation.Type];

        var activity = source.StartActivity(operationType, ActivityKind.Client);

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Operation.Type, operationType);

        if (!string.IsNullOrEmpty(operation.Name))
        {
            activity.SetTag(GraphQL.Operation.Name, operation.Name);
        }

        activity.SetTag(GraphQL.Document.Hash, $"sha256:{operation.Hash}");

        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            activity.SetTag(GraphQL.Source.Name, schemaName);
        }

        return new ExecuteSourceSchemaRequestSpan(activity, context, node, schemaName, enricher);
    }

    protected override void OnComplete()
    {
        if (Activity.Status != ActivityStatusCode.Error)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichSourceSchemaRequest(context, node, schemaName, Activity);
    }
}

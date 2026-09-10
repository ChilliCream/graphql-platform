using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecutePlanNodeSpan(
    Activity activity,
    OperationPlanContext context,
    ExecutionNode node,
    string? schemaName,
    FusionActivityEnricher enricher) : SpanBase(activity)
{
    internal static FrozenDictionary<ExecutionNodeType, string> KindValues { get; } =
        new Dictionary<ExecutionNodeType, string>
        {
            [ExecutionNodeType.Operation] = GraphQL.Operation.Step.KindValues.Operation,
            [ExecutionNodeType.OperationBatch] = GraphQL.Operation.Step.KindValues.OperationBatch,
            [ExecutionNodeType.EventStream] = GraphQL.Operation.Step.KindValues.EventStream,
            [ExecutionNodeType.Introspection] = GraphQL.Operation.Step.KindValues.Introspection,
            [ExecutionNodeType.Node] = GraphQL.Operation.Step.KindValues.Node
        }.ToFrozenDictionary();

    public static ExecutePlanNodeSpan? Start(
        ActivitySource source,
        OperationPlanContext context,
        ExecutionNode node,
        string? schemaName,
        FusionActivityEnricher enricher)
    {
        var activity = context.RequestContext.Features.TryGet<ExecuteOperationSpan>(out var executeOperationSpan)
            ? source.StartActivity(
                "GraphQL Step Execution",
                ActivityKind.Internal,
                executeOperationSpan.Activity.Context)
            : source.StartActivity("GraphQL Step Execution");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        var operation = context.OperationPlan.Operation;
        activity.EnrichOperation(operation.Definition.Operation, operation.Name);
        activity.EnrichDocumentInfo(context.RequestContext.OperationDocumentInfo);

        activity.SetTag(GraphQL.Operation.Step.Id, node.Id.ToString(CultureInfo.InvariantCulture));

        // An execution node type that has no mapped kind value produces an untagged
        // span instead of failing the node's execution.
        if (KindValues.TryGetValue(node.Type, out var kind))
        {
            activity.SetTag(GraphQL.Operation.Step.Kind, kind);
        }

        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);

        SetSourceSchemaTags(activity, node, schemaName);

        return new ExecutePlanNodeSpan(activity, context, node, schemaName, enricher);
    }

    internal static void SetSourceSchemaTags(
        Activity activity,
        ExecutionNode node,
        string? schemaName)
    {
        switch (node)
        {
            case OperationExecutionNode operationExecutionNode:
                SetSourceSchemaTags(activity, operationExecutionNode.Operation, schemaName);
                break;

            case ApolloOperationExecutionNode apolloOperationExecutionNode:
                SetSourceSchemaTags(activity, apolloOperationExecutionNode.Operation, schemaName);
                break;

            case OperationBatchExecutionNode operationBatchExecutionNode:
                SetSourceSchemaBatchTags(
                    activity,
                    schemaName,
                    operationBatchExecutionNode.Operations.Length);
                break;

            case ApolloOperationBatchExecutionNode apolloOperationBatchExecutionNode:
                SetSourceSchemaBatchTags(
                    activity,
                    schemaName,
                    apolloOperationBatchExecutionNode.Operations.Length);
                break;
        }
    }

    protected override void OnComplete()
    {
        if (Activity.Status == ActivityStatusCode.Error)
        {
            enricher.EnrichExecutePlanNode(context, node, schemaName, Activity);
            return;
        }

        // A step that was still in flight when the request was cancelled did not
        // complete successfully, so it is left Unset instead of being forced to
        // Ok, mirroring the request and subscription event spans. A step that
        // finished before any cancellation is reported as Ok.
        if (!context.RequestContext.RequestAborted.IsCancellationRequested)
        {
            Activity.SetStatus(ActivityStatusCode.Ok);
        }

        enricher.EnrichExecutePlanNode(context, node, schemaName, Activity);
    }

    private static void SetSourceSchemaTags(Activity activity, OperationSourceText operation, string? schemaName)
    {
        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            activity.SetTag(GraphQL.SourceSchema.Name, schemaName);
        }

        activity.SetTag(GraphQL.SourceSchema.Operation.Name, operation.Name);
        activity.SetTag(GraphQL.SourceSchema.Operation.Hash, $"sha256:{operation.Hash.Sha256}");
    }

    private static void SetSourceSchemaBatchTags(
        Activity activity,
        string? schemaName,
        int operationCount)
    {
        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            activity.SetTag(GraphQL.SourceSchema.Name, schemaName);
        }

        activity.SetTag(GraphQL.SourceSchema.Batch.OperationCount, operationCount);
    }
}

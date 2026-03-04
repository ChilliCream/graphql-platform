using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Diagnostics;

internal sealed class ExecutePlanNodeSpan(Activity activity) : SpanBase(activity)
{
    private static FrozenDictionary<ExecutionNodeType, string> KindValues { get; } =
        new Dictionary<ExecutionNodeType, string>
        {
            [ExecutionNodeType.Operation] = GraphQL.Operation.Step.KindValues.Operation,
            [ExecutionNodeType.OperationBatch] = GraphQL.Operation.Step.KindValues.OperationBatch,
            [ExecutionNodeType.Introspection] = GraphQL.Operation.Step.KindValues.Introspection,
            [ExecutionNodeType.Node] = GraphQL.Operation.Step.KindValues.Node
        }.ToFrozenDictionary();

    public static ExecutePlanNodeSpan? Start(
        ActivitySource source,
        OperationPlanContext context,
        ExecutionNode node,
        string? schemaName)
    {
        var activity = source.StartActivity("GraphQL Step Execution");

        if (activity is null)
        {
            return null;
        }

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        activity.SetTag(GraphQL.Operation.Step.Id, node.Id.ToString(CultureInfo.InvariantCulture));
        activity.SetTag(GraphQL.Operation.Step.Kind, KindValues[node.Type]);
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);

        if (node is OperationExecutionNode operationExecutionNode)
        {
            SetSourceSchemaTags(activity, operationExecutionNode.Operation, schemaName);
        }
        else if (node is OperationBatchExecutionNode batchExecutionNode)
        {
            SetSourceSchemaTags(activity, batchExecutionNode.Operation, schemaName);
        }

        return new ExecutePlanNodeSpan(activity);
    }

    private static void SetSourceSchemaTags(Activity activity, OperationSourceText operation, string? schemaName)
    {
        if (!string.IsNullOrWhiteSpace(schemaName))
        {
            activity.SetTag(GraphQL.Source.Name, schemaName);
        }

        activity.SetTag(GraphQL.Source.Operation.Name, operation.Name);
        activity.SetTag(GraphQL.Source.Operation.Kind, GraphQL.Operation.TypeValues[operation.Type]);
        activity.SetTag(GraphQL.Source.Operation.Hash, $"sha256:{operation.Hash}");
    }
}

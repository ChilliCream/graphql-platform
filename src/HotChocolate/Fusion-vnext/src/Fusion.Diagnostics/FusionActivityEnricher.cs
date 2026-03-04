using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// The activity enricher is used to add information to the activity spans.
/// You can inherit from this class and override the enricher methods to provide more or
/// less information.
/// </summary>
public class FusionActivityEnricher(
    ObjectPool<StringBuilder> stringBuilderPool,
    InstrumentationOptions options) : ActivityEnricherBase(stringBuilderPool, options)
{
    private static FrozenDictionary<ExecutionNodeType, string> KindValues { get; } =
        new Dictionary<ExecutionNodeType, string>
        {
            [ExecutionNodeType.Operation] = GraphQL.Operation.Step.KindValues.Operation,
            [ExecutionNodeType.OperationBatch] = GraphQL.Operation.Step.KindValues.OperationBatch,
            [ExecutionNodeType.Introspection] = GraphQL.Operation.Step.KindValues.Introspection,
            [ExecutionNodeType.Node] = GraphQL.Operation.Step.KindValues.Node
        }.ToFrozenDictionary();

    public virtual void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();
        var operationDisplayName = CreateOperationDisplayName(context, plan);

        EnrichExecuteRequestCore(
            context,
            activity,
            operationDisplayName,
            plan?.Id,
            plan?.Operation.Definition.Operation,
            plan?.OperationName);
    }

    public virtual void EnrichParseDocument(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();

        EnrichParseDocumentCore(activity, plan?.Operation.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichValidateDocument(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();

        EnrichValidateDocumentCore(activity, plan?.Operation.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichCoerceVariables(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();

        EnrichCoerceVariablesCore(activity, plan?.Operation.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichPlanOperationScope(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();

        activity.DisplayName = "Plan Operation";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        EnrichWithTags(activity, plan?.Operation.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichExecuteOperation(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();
        activity.DisplayName =
            plan?.OperationName is { } op
                ? $"Execute Operation {op}"
                : "Execute Operation";

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        EnrichWithTags(activity, plan?.Operation.Definition, context.OperationDocumentInfo);
    }

    public virtual void EnrichExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName,
        Activity activity)
    {
        activity.DisplayName = $"Execute Operation Node ({schemaName})";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        EnrichOperationWithTags(
            activity,
            context.OperationPlan,
            node,
            node.Operation,
            context.RequestContext.OperationDocumentInfo,
            schemaName);
    }

    public virtual void EnrichExecuteOperationBatchNode(
        OperationPlanContext context,
        OperationBatchExecutionNode node,
        string schemaName,
        Activity activity)
    {
        activity.DisplayName = $"Execute Operation Batch Node ({schemaName})";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        EnrichOperationWithTags(
            activity,
            context.OperationPlan,
            node,
            node.Operation,
            context.RequestContext.OperationDocumentInfo,
            schemaName);
    }

    public virtual void EnrichExecuteNodeFieldNode(
        OperationPlanContext context,
        NodeFieldExecutionNode node,
        Activity activity)
    {
        activity.DisplayName = "Execute Node Field Node";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        EnrichNodeWithTags(activity, node, context.OperationPlan);
    }

    public virtual void EnrichExecuteIntrospectionNode(
        OperationPlanContext context,
        IntrospectionExecutionNode node,
        Activity activity)
    {
        activity.DisplayName = "Execute Introspection Node";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);

        EnrichNodeWithTags(activity, node, context.OperationPlan);
    }

    public virtual void EnrichExecutionNodeError(
        OperationPlanContext context,
        ExecutionNode node,
        System.Exception error,
        Activity activity)
        => activity.RecordException(error);

    public virtual void EnrichSourceSchemaError(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        System.Exception error,
        Activity activity)
        => activity.RecordException(error);

    protected virtual string? CreateOperationDisplayName(RequestContext context, OperationPlan? plan)
    {
        if (plan is null)
        {
            return null;
        }

        var selections = plan.Operation.RootSelectionSet.Selections;
        var names = new string[selections.Length];

        for (var i = 0; i < selections.Length; i++)
        {
            names[i] = selections[i].ResponseName;
        }

        return BuildOperationDisplayName(
            plan.Operation.Definition.Operation,
            plan.OperationName,
            names.Length,
            names);
    }

    private static void EnrichOperationWithTags(
        Activity activity,
        OperationPlan plan,
        ExecutionNode node,
        OperationSourceText operation,
        OperationDocumentInfo operationDocumentInfo,
        string schemaName)
    {
        EnrichNodeWithTags(activity, node, plan);

        activity.SetTag(GraphQL.Operation.Type, GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

        if (!string.IsNullOrEmpty(plan.OperationName))
        {
            activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
        }

        activity.SetTag(GraphQL.Document.Hash, operationDocumentInfo.Hash.Value);

        activity.SetTag(GraphQL.Source.Name, schemaName);
        activity.SetTag(GraphQL.Source.Operation.Name, operation.Name);
        activity.SetTag(GraphQL.Source.Operation.Kind, GraphQL.Operation.TypeValues[operation.Type]);
        activity.SetTag(GraphQL.Source.Operation.Hash, operation.Hash);
    }

    private static void EnrichNodeWithTags(Activity activity, ExecutionNode node, OperationPlan plan)
    {
        activity.SetTag(GraphQL.Operation.Step.Id, node.Id);
        activity.SetTag(GraphQL.Operation.Step.Kind, KindValues[node.Type]);
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, plan.Id);
    }
}

using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Diagnostics;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language.Utilities;
using OpenTelemetry.Trace;
using static HotChocolate.Diagnostics.SemanticConventions;

namespace HotChocolate.Fusion.Diagnostics;

/// <summary>
/// The activity enricher is used to add information to the activity spans.
/// You can inherit from this class and override the enricher methods to provide more or
/// less information.
/// </summary>
public class FusionActivityEnricher : ActivityEnricherBase
{
    private readonly InstrumentationOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="FusionActivityEnricher"/>.
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="options"></param>
    protected FusionActivityEnricher(
        ObjectPool<StringBuilder> stringBuilderPool,
        InstrumentationOptions options)
        : base(stringBuilderPool, options)
    {
        _options = options;
    }

    public virtual void EnrichExecuteRequest(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();
        var documentInfo = context.OperationDocumentInfo;
        var operationDisplayName = CreateOperationDisplayName(context, plan);

        if (_options.RenameRootActivity && operationDisplayName is not null)
        {
            UpdateRootActivityName(activity, operationDisplayName);
        }

        activity.DisplayName = operationDisplayName ?? "Execute Request";
        activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);
        activity.SetTag(GraphQL.Document.Valid, documentInfo.IsValidated);
        activity.SetTag(GraphQL.Operation.Id, plan?.Id);

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }
        }

        if (_options.IncludeDocument && documentInfo.Document is not null)
        {
            activity.SetTag(GraphQL.Document.Body, documentInfo.Document.Print());
        }

        if (context.Result is OperationResult { Errors: [_, ..] errors })
        {
            activity.SetTag(GraphQL.Errors.Count, errors.Count);
        }
    }

    protected virtual string? CreateOperationDisplayName(RequestContext context, OperationPlan? plan)
    {
        if (plan is null)
        {
            return null;
        }

        var displayName = StringBuilderPool.Get();

        try
        {
            var rootSelectionSet = plan.Operation.RootSelectionSet;
            var selectionCount = rootSelectionSet.Selections.Length;

            displayName.Append('{');
            displayName.Append(' ');

            foreach (var selection in rootSelectionSet.Selections[..Math.Min(3, selectionCount)])
            {
                if (displayName.Length > 2)
                {
                    displayName.Append(' ');
                }

                displayName.Append(selection.ResponseName);
            }

            if (rootSelectionSet.Selections.Length > 3)
            {
                displayName.Append(' ');
                displayName.Append('.');
                displayName.Append('.');
                displayName.Append('.');
            }

            displayName.Append(' ');
            displayName.Append('}');

            if (plan.OperationName is { } name)
            {
                displayName.Insert(0, ' ');
                displayName.Insert(0, name);
            }

            displayName.Insert(0, ' ');
            displayName.Insert(0, plan.Operation.Definition.Operation.ToString().ToLowerInvariant());

            return displayName.ToString();
        }
        finally
        {
            StringBuilderPool.Return(displayName);
        }
    }

    public virtual void EnrichParseDocument(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Parse Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Parse);

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        var plan = context.GetOperationPlan();

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichValidateDocument(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Validate Document";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Validate);

        if (_options.RenameRootActivity)
        {
            UpdateRootActivityName(activity, $"Begin {activity.DisplayName}");
        }

        var plan = context.GetOperationPlan();

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichCoerceVariables(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Coerce Variable";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.VariableCoercion);

        var plan = context.GetOperationPlan();

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichPlanOperationScope(RequestContext context, Activity activity)
    {
        activity.DisplayName = "Plan Operation";
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Plan);

        var plan = context.GetOperationPlan();

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }

            var documentInfo = context.OperationDocumentInfo;
            activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);
        }
    }

    public virtual void EnrichExecuteOperation(RequestContext context, Activity activity)
    {
        var plan = context.GetOperationPlan();
        activity.DisplayName =
            plan?.OperationName is { } op
                ? $"Execute Operation {op}"
                : "Execute Operation";

        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.Execute);

        if (plan is not null)
        {
            activity.SetTag(
                GraphQL.Operation.Type,
                GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

            if (!string.IsNullOrEmpty(plan.OperationName))
            {
                activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
            }
        }

        var documentInfo = context.OperationDocumentInfo;
        activity.SetTag(GraphQL.Document.Hash, documentInfo.Hash.Value);

        if (documentInfo.IsPersisted)
        {
            activity.SetTag(GraphQL.Document.Id, documentInfo.Id.Value);
        }
    }

    public virtual void EnrichExecuteOperationNode(
        OperationPlanContext context,
        OperationExecutionNode node,
        string schemaName,
        Activity activity)
    {
        activity.DisplayName = $"Execute Operation Node ({schemaName})";

        // Required
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);
        activity.SetTag(GraphQL.Operation.Step.Id, node.Id);

        // Recommended
        activity.SetTag(GraphQL.Operation.Step.Kind, node.Type.ToString());
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);

        var plan = context.OperationPlan;
        activity.SetTag(
            GraphQL.Operation.Type,
            GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

        if (!string.IsNullOrEmpty(plan.OperationName))
        {
            activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
        }

        activity.SetTag(GraphQL.Document.Hash, context.RequestContext.OperationDocumentInfo.Hash.Value);

        // Opt-in - source schema info
        activity.SetTag(GraphQL.Source.Name, schemaName);

        // Opt-in - source operation info
        var operation = node.Operation;
        activity.SetTag(GraphQL.Source.Operation.Name, operation.Name);
        activity.SetTag(GraphQL.Source.Operation.Kind, operation.Type.ToString().ToLowerInvariant());
        activity.SetTag(GraphQL.Source.Operation.Hash, operation.Hash);
    }

    public virtual void EnrichExecuteOperationBatchNode(
        OperationPlanContext context,
        ExecutionNode node,
        string schemaName,
        Activity activity)
    {
        activity.DisplayName = $"Execute Operation Batch Node ({schemaName})";

        // Required
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);
        activity.SetTag(GraphQL.Operation.Step.Id, node.Id);

        // Recommended
        activity.SetTag(GraphQL.Operation.Step.Kind, node.Type.ToString());
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);

        var plan = context.OperationPlan;
        activity.SetTag(
            GraphQL.Operation.Type,
            GraphQL.Operation.TypeValues[plan.Operation.Definition.Operation]);

        if (!string.IsNullOrEmpty(plan.OperationName))
        {
            activity.SetTag(GraphQL.Operation.Name, plan.OperationName);
        }

        activity.SetTag(GraphQL.Document.Hash, context.RequestContext.OperationDocumentInfo.Hash.Value);

        // Opt-in - source schema info
        activity.SetTag(GraphQL.Source.Name, schemaName);

        // Opt-in - source operation info (if available)
        if (node is OperationBatchExecutionNode batchNode)
        {
            var operation = batchNode.Operation;
            activity.SetTag(GraphQL.Source.Operation.Name, operation.Name);
            activity.SetTag(GraphQL.Source.Operation.Kind, operation.Type.ToString().ToLowerInvariant());
            activity.SetTag(GraphQL.Source.Operation.Hash, operation.Hash);
        }
    }

    public virtual void EnrichExecuteNodeFieldNode(
        OperationPlanContext context,
        NodeFieldExecutionNode node,
        Activity activity)
    {
        activity.DisplayName = "Execute Node Field Node";

        // Required
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);
        activity.SetTag(GraphQL.Operation.Step.Id, node.Id);

        // Recommended
        activity.SetTag(GraphQL.Operation.Step.Kind, node.Type.ToString());
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);
    }

    public virtual void EnrichExecuteIntrospectionNode(
        OperationPlanContext context,
        IntrospectionExecutionNode node,
        Activity activity)
    {
        activity.DisplayName = "Execute Introspection Node";

        // Required
        activity.SetTag(GraphQL.Processing.Type, GraphQL.Processing.TypeValues.StepExecute);
        activity.SetTag(GraphQL.Operation.Step.Id, node.Id);

        // Recommended
        activity.SetTag(GraphQL.Operation.Step.Kind, node.Type.ToString());
        activity.SetTag(GraphQL.Operation.Step.Plan.Id, context.OperationPlan.Id);
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
}

using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal static class PlannerCostEstimator
{
    internal const double OperationStepCost = 10.0;
    private const double RequirementLookupCost = 12.0;
    private const double InlineLikelyCost = 1.0;

    public static double EstimateTotalCost(
        PlanNode node,
        FusionSchemaDefinition schema)
    {
        var total = EstimatePathCost(node) + node.BacklogLowerBound + node.ResolutionCost;

        if (!node.Backlog.IsEmpty)
        {
            // We keep backlog lower bound maintenance fully incremental and only apply
            // a small context-sensitive adjustment for the next work item.
            total += EstimateContextualAdjustment(node.Backlog.Peek(), node, schema);
        }

        return total;
    }

    public static double EstimatePathCost(PlanNode node)
        => node.PathCost;

    public static double EstimateBacklogCost(
        PlanNode node,
        FusionSchemaDefinition schema)
        => node.BacklogLowerBound
            + (node.Backlog.IsEmpty
                ? 0.0
                : EstimateContextualAdjustment(node.Backlog.Peek(), node, schema));

    public static double EstimateWorkItemLowerBound(WorkItem workItem)
    {
        return workItem switch
        {
            OperationWorkItem => OperationStepCost,

            FieldRequirementWorkItem { Lookup: not null }
                => RequirementLookupCost,

            FieldRequirementWorkItem
                => InlineLikelyCost,

            NodeFieldWorkItem wi
                => OperationStepCost + EstimateNodeBranches(wi.NodeField) * OperationStepCost,

            NodeLookupWorkItem
                => OperationStepCost,

            _ => 1.0
        };
    }

    public static double EstimateWorkItemCost(
        WorkItem workItem,
        PlanNode current,
        FusionSchemaDefinition schema)
        => EstimateWorkItemLowerBound(workItem)
            + EstimateContextualAdjustment(workItem, current, schema);

    private static double EstimateContextualAdjustment(
        WorkItem workItem,
        PlanNode current,
        FusionSchemaDefinition schema)
    {
        return workItem switch
        {
            OperationWorkItem wi
                => EstimateSpillover(wi.SelectionSet, current.SchemaName, schema),

            FieldRequirementWorkItem { Lookup: null } wi
                => EstimateInlineLikelihoodAdjustment(wi, current),

            _ => 0.0
        };
    }

    private static double EstimateInlineLikelihoodAdjustment(
        FieldRequirementWorkItem workItem,
        PlanNode current)
    {
        var selectionSetId = workItem.Selection.SelectionSetId;

        for (var i = 0; i < current.Steps.Count; i++)
        {
            if (current.Steps[i] is OperationPlanStep step
                && step.SelectionSets.Contains(selectionSetId)
                && step.Id != workItem.StepId)
            {
                return 0.0;
            }
        }

        return OperationStepCost - InlineLikelyCost;
    }

    private static double EstimateSpillover(
        SelectionSet selectionSet,
        string targetSchema,
        FusionSchemaDefinition schema)
    {
        if (string.IsNullOrEmpty(targetSchema)
            || targetSchema.Equals("None", StringComparison.Ordinal))
        {
            return 0.0;
        }

        var spilloverSchemas = new HashSet<string>(StringComparer.Ordinal);

        CollectSpilloverSchemas(
            schema,
            selectionSet.Type,
            selectionSet.Selections,
            targetSchema,
            spilloverSchemas);

        return spilloverSchemas.Count * OperationStepCost;
    }

    private static void CollectSpilloverSchemas(
        FusionSchemaDefinition schema,
        ITypeDefinition type,
        IReadOnlyList<ISelectionNode> selections,
        string targetSchema,
        HashSet<string> spilloverSchemas)
    {
        if (type is not FusionComplexTypeDefinition complexType)
        {
            return;
        }

        foreach (var selection in selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    if (fieldNode.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                    if (!field.Sources.ContainsSchema(targetSchema))
                    {
                        foreach (var schemaName in field.Sources.Schemas)
                        {
                            if (!schemaName.Equals(targetSchema, StringComparison.Ordinal))
                            {
                                spilloverSchemas.Add(schemaName);
                            }
                        }
                    }
                    else if (field.Sources.TryGetMember(targetSchema, out var source)
                        && source.Requirements is not null)
                    {
                        spilloverSchemas.Add("__requirement__");
                    }

                    if (fieldNode.SelectionSet is not null)
                    {
                        CollectSpilloverSchemas(
                            schema,
                            field.Type.AsTypeDefinition(),
                            fieldNode.SelectionSet.Selections,
                            targetSchema,
                            spilloverSchemas);
                    }

                    break;

                case InlineFragmentNode inlineFragmentNode:
                    var typeCondition = type;

                    if (inlineFragmentNode.TypeCondition is not null)
                    {
                        typeCondition = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                    }

                    CollectSpilloverSchemas(
                        schema,
                        typeCondition,
                        inlineFragmentNode.SelectionSet.Selections,
                        targetSchema,
                        spilloverSchemas);

                    break;
            }
        }
    }

    private static int EstimateNodeBranches(NodeField nodeField)
    {
        if (nodeField.Field.SelectionSet is null)
        {
            return 0;
        }

        var typeConditions = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<SelectionSetNode>();
        stack.Push(nodeField.Field.SelectionSet);

        while (stack.TryPop(out var selectionSet))
        {
            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case InlineFragmentNode inlineFragmentNode:
                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeConditions.Add(inlineFragmentNode.TypeCondition.Name.Value);
                        }

                        stack.Push(inlineFragmentNode.SelectionSet);
                        break;

                    case FieldNode { SelectionSet: { } nestedSelectionSet }:
                        stack.Push(nestedSelectionSet);
                        break;
                }
            }
        }

        return typeConditions.Count;
    }
}

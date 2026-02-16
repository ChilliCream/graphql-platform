using System.Collections.Immutable;
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

    public static BacklogCostState AddWorkItemLowerBound(
        BacklogCostState backlogCostState,
        WorkItem workItem)
    {
        // This is the pure optimistic floor for remaining work:
        // we only add guarantees here.
        //
        // Any context-dependent adjustments (spillover/inline likelihood) stay outside this
        // incremental state so branch-and-bound pruning remains a safe lower bound.
        var operationLowerBound =
            backlogCostState.OperationLowerBound + EstimateWorkItemLowerBound(workItem);
        var maxProjectedDepth = backlogCostState.MaxProjectedDepth;
        var projectedOpsPerLevel = backlogCostState.ProjectedOpsPerLevel;

        if (TryGetProjectedOperationDepth(workItem, out var projectedDepth))
        {
            var current = projectedOpsPerLevel.GetValueOrDefault(projectedDepth, 0);
            projectedOpsPerLevel = projectedOpsPerLevel.SetItem(projectedDepth, current + 1);

            if (projectedDepth > maxProjectedDepth)
            {
                maxProjectedDepth = projectedDepth;
            }
        }

        return new BacklogCostState(operationLowerBound, maxProjectedDepth, projectedOpsPerLevel);
    }

    public static BacklogCostState RemoveWorkItemLowerBound(
        BacklogCostState backlogCostState,
        WorkItem workItem)
    {
        // Pop is the inverse of push: remove the optimistic floor for the popped work item
        // and update the projected shape only if that item represented a guaranteed operation.
        var operationLowerBound =
            Math.Max(0.0, backlogCostState.OperationLowerBound - EstimateWorkItemLowerBound(workItem));
        var maxProjectedDepth = backlogCostState.MaxProjectedDepth;
        var projectedOpsPerLevel = backlogCostState.ProjectedOpsPerLevel;

        if (TryGetProjectedOperationDepth(workItem, out var projectedDepth)
            && projectedOpsPerLevel.TryGetValue(projectedDepth, out var current))
        {
            if (current <= 1)
            {
                projectedOpsPerLevel = projectedOpsPerLevel.Remove(projectedDepth);

                if (projectedDepth == maxProjectedDepth)
                {
                    maxProjectedDepth = RecomputeMaxProjectedDepth(projectedOpsPerLevel);
                }
            }
            else
            {
                projectedOpsPerLevel = projectedOpsPerLevel.SetItem(projectedDepth, current - 1);
            }
        }

        return new BacklogCostState(operationLowerBound, maxProjectedDepth, projectedOpsPerLevel);
    }

    public static double EstimateBacklogLowerBound(PlanNode node)
        => EstimateBacklogLowerBound(
            node.Options,
            node.MaxDepth,
            node.OpsPerLevel,
            node.BacklogCostState);

    public static double EstimateBacklogLowerBound(
        OperationPlannerOptions options,
        int currentMaxDepth,
        ImmutableDictionary<int, int> currentOpsPerLevel,
        BacklogCostState backlogCostState)
    {
        // h(n) = guaranteed remaining operation floor
        //      + optimistic additional depth
        //      + optimistic additional excess fan-out.
        //
        // We compare projected backlog fan-out against already materialized ops at each depth
        // so we only charge the additional excess this backlog can still force.
        var total = backlogCostState.OperationLowerBound;

        if (backlogCostState.MaxProjectedDepth > currentMaxDepth)
        {
            total += (backlogCostState.MaxProjectedDepth - currentMaxDepth) * options.DepthWeight;
        }

        if (!backlogCostState.ProjectedOpsPerLevel.IsEmpty)
        {
            var threshold = options.FanoutPenaltyThreshold;
            var projectedAdditionalExcessFanout = 0;

            foreach (var (depth, projectedOps) in backlogCostState.ProjectedOpsPerLevel)
            {
                var currentOpsAtDepth = currentOpsPerLevel.GetValueOrDefault(depth, 0);
                var currentExcess = Math.Max(0, currentOpsAtDepth - threshold);
                var projectedExcess = Math.Max(0, currentOpsAtDepth + projectedOps - threshold);
                projectedAdditionalExcessFanout += projectedExcess - currentExcess;
            }

            if (projectedAdditionalExcessFanout > 0)
            {
                total += projectedAdditionalExcessFanout * options.ExcessFanoutWeight;
            }
        }

        return total;
    }

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

    private static bool TryGetProjectedOperationDepth(
        WorkItem workItem,
        out int projectedDepth)
    {
        switch (workItem)
        {
            case OperationWorkItem:
            case FieldRequirementWorkItem { Lookup: not null }:
            case NodeFieldWorkItem:
            case NodeLookupWorkItem:
                // These kinds all guarantee that at least one future operation step will exist.
                projectedDepth = workItem.EstimatedDepth;
                return true;

            default:
                // Inline field requirements can resolve without creating a new operation step.
                projectedDepth = 0;
                return false;
        }
    }

    private static int RecomputeMaxProjectedDepth(ImmutableDictionary<int, int> projectedOpsPerLevel)
    {
        var maxProjectedDepth = 0;

        foreach (var (depth, _) in projectedOpsPerLevel)
        {
            if (depth > maxProjectedDepth)
            {
                maxProjectedDepth = depth;
            }
        }

        return maxProjectedDepth;
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

                    if (schema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                    {
                        if (!fieldResolution.ContainsSchema(targetSchema))
                        {
                            foreach (var schemaName in fieldResolution.Schemas)
                            {
                                spilloverSchemas.Add(schemaName);
                            }
                        }
                        else if (fieldResolution.HasRequirements(targetSchema))
                        {
                            spilloverSchemas.Add("__requirement__");
                        }
                    }
                    else
                    {
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

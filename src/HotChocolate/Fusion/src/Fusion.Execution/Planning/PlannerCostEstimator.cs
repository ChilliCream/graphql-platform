using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The main purpose of the PlannerCostEstimator is to create a score for the OperationPlanner
/// that indicates how promising a plan branch is. It also tracks the estimated cost of
/// remaining work items and adjusts scores for field spillover across schemas and for
/// requirements that can be inlined into existing steps.
/// </summary>
internal static class PlannerCostEstimator
{
    private const double SpilloverOperationCost = 10.0;
    private const double InlineLikelyCost = 1.0;

    /// <summary>
    /// Fake schema name added to the spillover set when a field can be served by the target
    /// schema but has requirements — counts as one extra operation in the spillover estimate.
    /// </summary>
    private const string RequirementSpilloverMarker = "__requirement__";

    /// <summary>
    /// Computes the priority score for a plan node that indicates how promising this plan branch is.
    /// Lower scores are explored first. The score adds up the path cost, remaining cost, resolution cost,
    /// and a small adjustment for the next backlog item (spillover or inline likelihood).
    /// </summary>
    public static double ScoreNode(
        PlanNode node,
        FusionSchemaDefinition schema)
    {
        var score = node.TotalCost;

        if (!node.Backlog.IsEmpty)
        {
            // The remaining cost is a cheap optimistic floor that stays safe for pruning.
            // To improve ranking without re-scoring the entire backlog on every enqueue,
            // we only apply a context-aware tweak (spillover / inline likelihood) to the
            // next item about to be processed.
            score += node.Backlog.Peek() switch
            {
                // Spillover: how many fields in this selection set can't be served by
                // the target schema and will need additional operations on other schemas.
                OperationWorkItem wi
                    => EstimateSpillover(wi.SelectionSet, node.SchemaName, schema),

                // Inline likelihood: if a requirement can likely be folded into an
                // existing step we lower its cost; otherwise we charge the cost of a full operation.
                FieldRequirementWorkItem { Lookup: null } wi
                    => EstimateInlineCost(wi, node),

                _ => 0.0
            };
        }

        return score;
    }

    private static double EstimateSpillover(
        SelectionSet selectionSet,
        string targetSchema,
        FusionSchemaDefinition schema)
    {
        if (string.IsNullOrEmpty(targetSchema)
            || targetSchema.Equals(PlanNode.UnresolvedSchemaName, StringComparison.Ordinal))
        {
            return 0.0;
        }

        var spilloverSchemas = new HashSet<string>(StringComparer.Ordinal);

        // We walks the selection set and collect the distinct set of schemas that the target
        // schema cannot serve, plus a marker if any field has requirements on the target schema.
        CollectSpilloverSchemas(
            schema,
            selectionSet.Type,
            selectionSet.Selections,
            targetSchema,
            spilloverSchemas);

        // The count of the spilloverSchemas set drives the spillover cost estimate.
        return spilloverSchemas.Count * SpilloverOperationCost;
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

                    if (!schema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                    {
                        continue;
                    }

                    // Field can't be served by the target schema — collect all alternative schemas.
                    if (!fieldResolution.ContainsSchema(targetSchema))
                    {
                        foreach (var schemaName in fieldResolution.Schemas)
                        {
                            spilloverSchemas.Add(schemaName);
                        }
                    }
                    // Field is on the target schema but has requirements — count that as
                    // one extra operation since a requirement lookup will be needed.
                    else if (fieldResolution.HasRequirements(targetSchema))
                    {
                        spilloverSchemas.Add(RequirementSpilloverMarker);
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
                        typeCondition = schema.Types.GetType(
                            inlineFragmentNode.TypeCondition.Name.Value,
                            allowInaccessibleFields: true);
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

    private static double EstimateInlineCost(
        FieldRequirementWorkItem workItem,
        PlanNode current)
    {
        var selectionSetId = workItem.Selection.SelectionSetId;
        var consumerStepId = workItem.Consumer is StepConsumer stepConsumer ? stepConsumer.StepId : -1;

        for (var i = 0; i < current.Steps.Count; i++)
        {
            if (current.Steps[i] is OperationPlanStep step
                && step.SelectionSets.Contains(selectionSetId)
                && step.Id != consumerStepId)
            {
                return 0.0;
            }
        }

        return SpilloverOperationCost - InlineLikelyCost;
    }

    /// <summary>
    /// Adds the operations and projected depth a work item is guaranteed to produce
    /// to the backlog cost tracking.
    /// </summary>
    public static BacklogCost AddWorkItemCost(
        BacklogCost backlogCost,
        WorkItem workItem)
    {
        // A work item contributes the minimum over every alternative it can become.
        var minimumOperationCount = backlogCost.MinimumOperationCount + GetGuaranteedOperationCount(workItem);
        var maxProjectedDepth = backlogCost.MaxProjectedDepth;
        var projectedOpsPerLevel = backlogCost.ProjectedOpsPerLevel;

        if (TryGetProjectedOperationDepth(workItem, out var projectedDepth))
        {
            var current = projectedOpsPerLevel.GetValueOrDefault(projectedDepth, 0);
            projectedOpsPerLevel = projectedOpsPerLevel.SetItem(projectedDepth, current + 1);

            if (projectedDepth > maxProjectedDepth)
            {
                maxProjectedDepth = projectedDepth;
            }
        }

        return new BacklogCost(minimumOperationCount, maxProjectedDepth, projectedOpsPerLevel);
    }

    /// <summary>
    /// Subtracts the operations and projected depth a work item is guaranteed to produce
    /// from the backlog cost tracking.
    /// </summary>
    public static BacklogCost RemoveWorkItemCost(
        BacklogCost backlogCost,
        WorkItem workItem)
    {
        var minimumOperationCount = Math.Max(
            0,
            backlogCost.MinimumOperationCount - GetGuaranteedOperationCount(workItem));
        var maxProjectedDepth = backlogCost.MaxProjectedDepth;
        var projectedOpsPerLevel = backlogCost.ProjectedOpsPerLevel;

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

        return new BacklogCost(minimumOperationCount, maxProjectedDepth, projectedOpsPerLevel);
    }

    private static int GetGuaranteedOperationCount(WorkItem workItem)
    {
        return workItem switch
        {
            // Each of these always adds a new operation step to the plan.
            OperationWorkItem or NodeFieldWorkItem or NodeLookupWorkItem => 1,

            // A requirement lookup can merge into an existing step and an inline
            // requirement can be satisfied without any operation.
            _ => 0
        };
    }

    private static bool TryGetProjectedOperationDepth(
        WorkItem workItem,
        out int projectedDepth)
    {
        switch (workItem)
        {
            // A path lookup that has no lookup chosen yet can still become a root work item at
            // depth 1, so it does not guarantee a step at any depth.
            case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: null }:
                projectedDepth = 0;
                return false;

            case OperationWorkItem:
            case NodeFieldWorkItem:
            case NodeLookupWorkItem:
                // These kinds all guarantee that a new operation step exists at this depth.
                projectedDepth = workItem.EstimatedDepth;
                return true;

            default:
                // Requirement work items can merge into an existing step or resolve inline
                // without creating a new operation step.
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

    /// <summary>
    /// Computes a lower bound for the cost of completing all remaining backlog work by combining
    /// the guaranteed operations with penalties for additional depth and excess fan-out.
    /// </summary>
    public static double EstimateRemainingCost(
        OperationPlannerOptions options,
        int currentMaxDepth,
        ImmutableDictionary<int, int> currentOpsPerLevel,
        BacklogCost backlogCost)
    {
        // h(n) = guaranteed operations * operation weight
        //      + additional depth penalty
        //      + additional excess fan-out penalty.
        //
        // We compare projected backlog fan-out against already materialized ops at each depth
        // so we only charge the additional excess this backlog can still force.
        var total = backlogCost.MinimumOperationCount * options.OperationWeight;

        if (backlogCost.MaxProjectedDepth > currentMaxDepth)
        {
            total += (backlogCost.MaxProjectedDepth - currentMaxDepth) * options.DepthWeight;
        }

        if (!backlogCost.ProjectedOpsPerLevel.IsEmpty)
        {
            var threshold = options.FanoutPenaltyThreshold;
            var projectedAdditionalExcessFanout = 0;

            foreach (var (depth, projectedOps) in backlogCost.ProjectedOpsPerLevel)
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
}

using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Plans each <see cref="DeferSubPlanDescriptor"/> independently and
    /// materializes one <see cref="ExecutionSubPlan"/> per unique
    /// <c>DeferUsageSet</c>. The resulting subplan carries its own compiled
    /// <see cref="Operation"/> together with the
    /// <see cref="DeferUsage"/> set that keys it (sorted by <see cref="DeferUsage.Id"/>
    /// for stable serialization). The subplan's data is delivered on the wire
    /// to every <see cref="DeferUsage"/> in its
    /// <see cref="ExecutionSubPlan.DeliveryGroups"/>.
    /// </summary>
    private ImmutableArray<ExecutionSubPlan> PlanDeferredSubPlans(
        string id,
        string hash,
        DeferSplitResult splitResult,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        if (splitResult.SubPlanDescriptors.IsEmpty)
        {
            return [];
        }

        var subPlans = ImmutableArray.CreateBuilder<ExecutionSubPlan>(splitResult.SubPlanDescriptors.Length);

        for (var i = 0; i < splitResult.SubPlanDescriptors.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var descriptor = splitResult.SubPlanDescriptors[i];
            var subPlanId = i;

            // Plan the subplan as an independent query.
            var (deferredPlanSteps, deferredInternalOp) = PlanDeferredSubPlan(
                id,
                descriptor,
                subPlanId,
                emitPlannerEvents,
                cancellationToken);

            // Build execution nodes for this subplan.
            var (rootNodes, allNodes) = BuildDeferredExecutionNodes(
                deferredInternalOp ?? descriptor.Operation,
                deferredPlanSteps);

            // Compile a standalone Operation for this subplan's result mapping.
            var compiledOp = deferredInternalOp ?? descriptor.Operation;
            compiledOp = AddTypeNameToAbstractSelections(
                compiledOp,
                _schema.GetOperationType(compiledOp.Operation));
            var deferredOperation = _operationCompiler.Compile(
                id + "#defer_" + subPlanId,
                hash + "#defer_" + subPlanId,
                compiledOp);

            // The descriptor carries the DeferUsageSet already sorted by Id
            // (see DeferOperationRewriter.Split); capture it as this subplan's
            // delivery groups.
            var subPlan = new ExecutionSubPlan(
                deferredOperation,
                rootNodes,
                allNodes,
                descriptor.DeferUsageSet);

            subPlans.Add(subPlan);
        }

        return subPlans.ToImmutable();
    }

    /// <summary>
    /// Plans a single subplan using the A* planner.
    /// </summary>
    private (ImmutableList<PlanStep> Steps, OperationDefinitionNode? InternalOp) PlanDeferredSubPlan(
        string operationId,
        DeferSubPlanDescriptor descriptor,
        int subPlanId,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        var deferredOperation = descriptor.Operation;

        var index = SelectionSetIndexer.Create(deferredOperation);

        var (node, selectionSet) = CreateQueryPlanBase(deferredOperation, "defer", index);

        if (node.Backlog.IsEmpty)
        {
            return ([], null);
        }

        var possiblePlans = new PlanQueue(_schema);

        foreach (var (schemaName, resolutionCost) in _schema.GetPossibleSchemas(selectionSet))
        {
            possiblePlans.Enqueue(
                node with
                {
                    SchemaName = schemaName,
                    ResolutionCost = resolutionCost
                });
        }

        if (possiblePlans.Count < 1)
        {
            possiblePlans.Enqueue(node);
        }

        var plan = Plan(operationId + "#defer_" + subPlanId, possiblePlans, emitPlannerEvents, cancellationToken);

        if (!plan.HasValue)
        {
            return ([], null);
        }

        return (plan.Value.Steps, plan.Value.InternalOperationDefinition);
    }

    /// <summary>
    /// Builds execution nodes for a subplan's plan steps.
    /// </summary>
    private (ImmutableArray<ExecutionNode> RootNodes, ImmutableArray<ExecutionNode> AllNodes) BuildDeferredExecutionNodes(
        OperationDefinitionNode deferredOperation,
        ImmutableList<PlanStep> planSteps)
    {
        if (planSteps.Count == 0)
        {
            return ([], []);
        }

        var ctx = new ExecutionPlanBuildContext();
        var hasVariables = deferredOperation.VariableDefinitions.Count > 0;

        planSteps = TransformPlanSteps(planSteps, deferredOperation);
        IndexDependencies(planSteps, ctx);
        BuildExecutionNodes(planSteps, ctx, _schema, hasVariables, CancellationToken.None);
        MergeAndBatchOperations(ctx, _options.EnableRequestGrouping, _options.MergePolicy);
        WireExecutionDependencies(ctx);

        var rootNodes = planSteps
            .Where(t => !ctx.DependenciesByStepId.ContainsKey(t.Id) && ctx.ExecutionNodes.ContainsKey(t.Id))
            .Select(t => ctx.ExecutionNodes[t.Id])
            .ToImmutableArray();

        var allNodes = ctx.ExecutionNodes
            .OrderBy(t => t.Key)
            .Select(t => t.Value)
            .ToImmutableArray();

        foreach (var node in allNodes)
        {
            node.Seal();
        }

        return (rootNodes, allNodes);
    }
}

using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Splits the operation at @defer boundaries, plans each deferred fragment
    /// independently, and builds <see cref="DeferredExecutionGroup"/>s.
    /// </summary>
    private ImmutableArray<DeferredExecutionGroup> PlanDeferredGroups(
        string id,
        string hash,
        string shortHash,
        DeferSplitResult splitResult,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        if (splitResult.DeferredFragments.IsEmpty)
        {
            return [];
        }

        var groups = ImmutableArray.CreateBuilder<DeferredExecutionGroup>(splitResult.DeferredFragments.Length);

        // Map from DeferredFragmentDescriptor to DeferredExecutionGroup for parent lookups
        var descriptorToGroup = new Dictionary<int, DeferredExecutionGroup>();

        foreach (var descriptor in splitResult.DeferredFragments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Plan the deferred fragment as an independent query
            var (deferredPlanSteps, deferredInternalOp) = PlanDeferredFragment(
                id,
                descriptor,
                emitPlannerEvents,
                cancellationToken);

            // Build execution nodes for this deferred group
            var (rootNodes, allNodes) = BuildDeferredExecutionNodes(
                deferredInternalOp ?? descriptor.Operation,
                deferredPlanSteps);

            // Compile a standalone Operation for this deferred group's result mapping.
            var compiledOp = deferredInternalOp ?? descriptor.Operation;
            compiledOp = AddTypeNameToAbstractSelections(
                compiledOp,
                _schema.GetOperationType(compiledOp.Operation));
            var deferredOperation = _operationCompiler.Compile(
                id + "#defer_" + descriptor.DeferId,
                hash + "#defer_" + descriptor.DeferId,
                compiledOp);

            // Convert the field path to a SelectionPath
            var path = BuildSelectionPath(descriptor.Path);

            // Look up parent group
            DeferredExecutionGroup? parentGroup = null;
            if (descriptor.Parent is not null)
            {
                descriptorToGroup.TryGetValue(descriptor.Parent.DeferId, out parentGroup);
            }

            var group = new DeferredExecutionGroup(
                descriptor.DeferId,
                descriptor.Label,
                path,
                descriptor.IfVariable,
                parentGroup,
                deferredOperation,
                rootNodes,
                allNodes);

            descriptorToGroup[descriptor.DeferId] = group;
            groups.Add(group);
        }

        return groups.ToImmutable();
    }

    /// <summary>
    /// Plans a single deferred fragment using the A* planner.
    /// </summary>
    private (ImmutableList<PlanStep> Steps, OperationDefinitionNode? InternalOp) PlanDeferredFragment(
        string operationId,
        DeferredFragmentDescriptor descriptor,
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

        var plan = Plan(operationId + "#defer_" + descriptor.DeferId, possiblePlans, emitPlannerEvents, cancellationToken);

        if (!plan.HasValue)
        {
            return ([], null);
        }

        return (plan.Value.Steps, plan.Value.InternalOperationDefinition);
    }

    /// <summary>
    /// Builds execution nodes for a deferred fragment's plan steps.
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

    private static SelectionPath BuildSelectionPath(ImmutableArray<FieldPathSegment> path)
    {
        if (path.IsEmpty)
        {
            return SelectionPath.Root;
        }

        var builder = SelectionPath.Root;

        for (var i = 0; i < path.Length; i++)
        {
            builder = builder.AppendField(path[i].ResponseName);
        }

        return builder;
    }
}

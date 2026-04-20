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
        _ = shortHash;

        if (splitResult.DeferredFragments.IsEmpty)
        {
            return [];
        }

        var groups = ImmutableArray.CreateBuilder<DeferredExecutionGroup>(splitResult.DeferredFragments.Length);

        // Map from DeferredFragmentDescriptor to DeferredExecutionGroup for parent lookups
        var descriptorToGroup = new Dictionary<int, DeferredExecutionGroup>();

        // Precompute sibling-defer overlap per descriptor. Two descriptors are
        // siblings when they share the same Parent and attach at the same Path.
        // If a response name appears in multiple siblings, all competing DeferIds
        // are recorded (in declaration order) so the executor can resolve the
        // primary at runtime and drop duplicates from the losing payloads.
        var siblingOverlap = ComputeSiblingOverlap(splitResult.DeferredFragments);

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

            siblingOverlap.TryGetValue(descriptor.DeferId, out var overlapForGroup);

            var group = new DeferredExecutionGroup(
                descriptor.DeferId,
                descriptor.Label,
                path,
                descriptor.IfVariable,
                parentGroup,
                deferredOperation,
                rootNodes,
                allNodes,
                overlapForGroup);

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

    /// <summary>
    /// Computes, per defer descriptor, the response names that are also selected
    /// by at least one sibling defer (same Parent, same Path). For each such name
    /// the returned entry lists the <see cref="DeferredFragmentDescriptor.DeferId"/>
    /// of every sibling that selects it (including this descriptor itself) in
    /// declaration order.
    ///
    /// Returns an empty dictionary when no sibling overlap exists anywhere (the
    /// fast path for well-behaved queries).
    /// </summary>
    private static Dictionary<int, ImmutableDictionary<string, ImmutableArray<int>>?> ComputeSiblingOverlap(
        ImmutableArray<DeferredFragmentDescriptor> descriptors)
    {
        var result = new Dictionary<int, ImmutableDictionary<string, ImmutableArray<int>>?>();

        // Group descriptors by sibling key (Parent.DeferId, Path). Descriptors in
        // the same group compete for overlapping response names.
        var siblingGroups = new Dictionary<(int ParentDeferId, ImmutableArray<FieldPathSegment> Path), List<DeferredFragmentDescriptor>>(
            SiblingKeyComparer.Instance);

        for (var i = 0; i < descriptors.Length; i++)
        {
            var descriptor = descriptors[i];
            var key = (descriptor.Parent?.DeferId ?? -1, descriptor.Path);

            if (!siblingGroups.TryGetValue(key, out var list))
            {
                list = [];
                siblingGroups[key] = list;
            }

            list.Add(descriptor);
        }

        foreach (var (_, siblings) in siblingGroups)
        {
            if (siblings.Count < 2)
            {
                // No siblings — the descriptor is alone at this (Parent, Path).
                continue;
            }

            // Build responseName -> ordered list of DeferIds that select it.
            var overlapByName = new Dictionary<string, ImmutableArray<int>.Builder>(StringComparer.Ordinal);

            // Preserve declaration order: sort siblings by DeferId ascending,
            // matching the order in which _nextDeferId was incremented.
            siblings.Sort(static (a, b) => a.DeferId.CompareTo(b.DeferId));

            foreach (var descriptor in siblings)
            {
                foreach (var responseName in descriptor.TopLevelResponseNames)
                {
                    if (!overlapByName.TryGetValue(responseName, out var builder))
                    {
                        builder = ImmutableArray.CreateBuilder<int>();
                        overlapByName[responseName] = builder;
                    }

                    builder.Add(descriptor.DeferId);
                }
            }

            // Keep only names that are actually shared across multiple siblings.
            List<KeyValuePair<string, ImmutableArray<int>>>? shared = null;

            foreach (var (name, builder) in overlapByName)
            {
                if (builder.Count > 1)
                {
                    shared ??= [];
                    shared.Add(new KeyValuePair<string, ImmutableArray<int>>(name, builder.ToImmutable()));
                }
            }

            if (shared is null)
            {
                continue;
            }

            // Publish the same overlap map to every sibling in this group that
            // selects at least one shared name. The map is immutable and shared.
            var sharedMap = ImmutableDictionary.CreateRange(StringComparer.Ordinal, shared);

            foreach (var descriptor in siblings)
            {
                if (!DescriptorHasAny(descriptor, sharedMap))
                {
                    continue;
                }

                result[descriptor.DeferId] = sharedMap;
            }
        }

        return result;
    }

    private static bool DescriptorHasAny(
        DeferredFragmentDescriptor descriptor,
        ImmutableDictionary<string, ImmutableArray<int>> overlap)
    {
        foreach (var responseName in descriptor.TopLevelResponseNames)
        {
            if (overlap.ContainsKey(responseName))
            {
                return true;
            }
        }

        return false;
    }

    private sealed class SiblingKeyComparer : IEqualityComparer<(int ParentDeferId, ImmutableArray<FieldPathSegment> Path)>
    {
        public static readonly SiblingKeyComparer Instance = new();

        public bool Equals(
            (int ParentDeferId, ImmutableArray<FieldPathSegment> Path) x,
            (int ParentDeferId, ImmutableArray<FieldPathSegment> Path) y)
        {
            if (x.ParentDeferId != y.ParentDeferId)
            {
                return false;
            }

            if (x.Path.Length != y.Path.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Path.Length; i++)
            {
                if (!x.Path[i].Equals(y.Path[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode((int ParentDeferId, ImmutableArray<FieldPathSegment> Path) obj)
        {
            var hash = obj.ParentDeferId;

            for (var i = 0; i < obj.Path.Length; i++)
            {
                hash = HashCode.Combine(hash, obj.Path[i]);
            }

            return hash;
        }
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

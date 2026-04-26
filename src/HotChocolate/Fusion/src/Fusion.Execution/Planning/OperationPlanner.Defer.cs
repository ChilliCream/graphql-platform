using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    /// <summary>
    /// Plans each <see cref="IncrementalPlanDescriptor"/> and routes each
    /// sub-plan's self-fetch requirements into its enclosing scope's
    /// variable-flow graph. Each iteration may mutate the enclosing scope's
    /// step list (via the context graph), so subsequent siblings and nested
    /// descriptors observe the cumulative parent-scope state. Must run
    /// BEFORE the root operation is compiled so the root internal operation
    /// reflects every field absorbed for defer sub-plans by the time compile
    /// consumes it. Execution-node construction for each sub-plan happens in
    /// a separate post-compile pass via
    /// <see cref="BuildIncrementalPlans"/>.
    /// </summary>
    private ImmutableArray<DeferRoutingState> RouteIncrementalPlans(
        string id,
        DeferSplitResult splitResult,
        PlanContextGraph contextGraph,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        if (splitResult.SubPlanDescriptors.IsEmpty)
        {
            return [];
        }

        var routingStates = ImmutableArray.CreateBuilder<DeferRoutingState>(
            splitResult.SubPlanDescriptors.Length);

        for (var i = 0; i < splitResult.SubPlanDescriptors.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var descriptor = splitResult.SubPlanDescriptors[i];

            var parentContext = contextGraph.GetParentContext(descriptor);
            var subPlanResult = PlanIncrementalPlan(
                id,
                descriptor,
                i,
                emitPlannerEvents,
                cancellationToken);

            var rewrittenSubPlan = ApplyDeferRequirementsToParent(
                descriptor,
                subPlanResult.Steps,
                parentContext,
                contextGraph);

            var registeredInternalOp = subPlanResult.InternalOperationDefinition ?? descriptor.Operation;
            contextGraph.RegisterDeferContext(
                descriptor,
                rewrittenSubPlan,
                SelectionSetIndexer.Create(registeredInternalOp),
                registeredInternalOp);

            routingStates.Add(new DeferRoutingState(descriptor, i));
        }

        return routingStates.ToImmutable();
    }

    /// <summary>
    /// Compiles each routed sub-plan and builds its execution-node graph.
    /// Must run AFTER the root operation is compiled; the root's compiled
    /// operation already carries every field the sub-plans need because the
    /// routing pass inlined those fields into the root internal operation
    /// before compile.
    /// </summary>
    private ImmutableArray<IncrementalPlan> BuildIncrementalPlans(
        string id,
        string hash,
        ImmutableArray<DeferRoutingState> routingStates,
        PlanContextGraph contextGraph,
        CancellationToken cancellationToken)
    {
        if (routingStates.IsDefaultOrEmpty)
        {
            return [];
        }

        var subPlans = ImmutableArray.CreateBuilder<IncrementalPlan>(routingStates.Length);

        foreach (var routingState in routingStates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var descriptor = routingState.Descriptor;

            // Read the final step list AND internal operation from the context
            // graph: both may have grown between the routing pass above and
            // this pass when a nested inner defer inlined a field or promoted
            // a step into the outer's scope.
            var finalSteps = contextGraph.GetRegisteredSteps(descriptor);
            var registeredInternalOp = contextGraph.GetRegisteredInternalOperation(descriptor);

            var (rootNodes, allNodes) = BuildDeferredExecutionNodes(
                registeredInternalOp,
                finalSteps);

            var compiledOp = AddTypeNameToAbstractSelections(
                registeredInternalOp,
                _schema.GetOperationType(registeredInternalOp.Operation));
            var deferredOperation = _operationCompiler.Compile(
                id + "#defer_" + routingState.Index,
                hash + "#defer_" + routingState.Index,
                compiledOp);

            var planScopeRequirements = descriptor.Requirements.Count == 0
                ? ImmutableArray<OperationRequirement>.Empty
                : [.. descriptor.Requirements.Values];

            var subPlan = new IncrementalPlan(
                deferredOperation,
                rootNodes,
                allNodes,
                descriptor.DeliveryGroupSet,
                planScopeRequirements);

            subPlans.Add(subPlan);
        }

        return subPlans.ToImmutable();
    }

    /// <summary>
    /// Captures the state produced by <see cref="RouteIncrementalPlans"/>
    /// that <see cref="BuildIncrementalPlans"/> needs to emit each sub-plan's
    /// compiled operation and execution-node graph.
    /// </summary>
    private readonly record struct DeferRoutingState(
        IncrementalPlanDescriptor Descriptor,
        int Index);

    /// <summary>
    /// Plans a single subplan using the A* planner. The sub-plan's self-fetch
    /// (if any) is kept intact here; requirement routing through the parent
    /// scope happens after planning via
    /// <see cref="ApplyDeferRequirementsToParent"/>.
    /// </summary>
    private DeferSubPlanResult PlanIncrementalPlan(
        string operationId,
        IncrementalPlanDescriptor descriptor,
        int subPlanId,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        var deferredOperation = descriptor.Operation;

        var index = SelectionSetIndexer.Create(deferredOperation);

        var (node, selectionSet) = CreateQueryPlanBase(deferredOperation, "defer", index);

        if (node.Backlog.IsEmpty)
        {
            return new DeferSubPlanResult([], null);
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
            return new DeferSubPlanResult([], null);
        }

        return new DeferSubPlanResult(
            plan.Value.Steps,
            plan.Value.InternalOperationDefinition);
    }

    /// <summary>
    /// Applies a deferred sub-plan's self-fetch requirements to the enclosing
    /// scope's variable-flow graph. The self-fetch is the sub-plan's first
    /// step when its sole purpose is to reproduce a parent selection so that
    /// later sub-plan steps can pull entity-key values from it. Under the
    /// variable-wiring model, the parent plan produces those values natively
    /// and the sub-plan drops the self-fetch; downstream sub-plan steps
    /// source the values via <see cref="OperationPlanStep.ParentDependencies"/>.
    /// </summary>
    /// <returns>
    /// The sub-plan step list with the self-fetch and any promoted steps
    /// removed, and downstream step ids renumbered to stay contiguous.
    /// Unchanged when the sub-plan has no self-fetch to absorb.
    /// </returns>
    private ImmutableList<PlanStep> ApplyDeferRequirementsToParent(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> subPlanSteps,
        ParentPlanContext parentContext,
        PlanContextGraph contextGraph)
    {
        // We can only route a sub-plan's self-fetch into the parent when the
        // self-fetch exists AND at least one downstream step reads from it.
        // Anything smaller has no routing opportunity.
        if (subPlanSteps.Count < 2
            || subPlanSteps[0] is not OperationPlanStep selfFetch
            || selfFetch.SchemaName is null
            || selfFetch.Dependents.IsEmpty)
        {
            return subPlanSteps;
        }

        // Collect the downstream steps in the defer sub-plan that depend on
        // the self-fetch directly. Each such step's Requirements dict tells
        // us what variables the sub-plan expects the parent to produce.
        var downstreamByStepId = new Dictionary<int, OperationPlanStep>();
        foreach (var dependentStepId in selfFetch.Dependents)
        {
            if (subPlanSteps.ById(dependentStepId) is OperationPlanStep dependentStep)
            {
                downstreamByStepId[dependentStepId] = dependentStep;
            }
        }

        if (downstreamByStepId.Count == 0)
        {
            return subPlanSteps;
        }

        // Route each requirement through a scope-walker: at each scope, try
        // same-subgraph inline first, then cross-subgraph promote; on both
        // failing, escalate to the next enclosing scope. The walker exits on
        // the first scope that successfully serves the requirement. At root
        // exhaustion, throw. Per-scope mutations (step-list + internal op)
        // are accumulated by scope identity and committed back to the graph
        // after the loop.
        var resolver = new ValueSelectionToSelectionSetRewriter(_schema);
        ScopeState? rootScopeState = null;
        var enclosingScopeStates = new Dictionary<IncrementalPlanDescriptor, ScopeState>();
        var lifted = new List<LiftedDeferRequirement>();
        var promotedSubPlanStepIds = new HashSet<int>();

        ScopeState ScopeStateFor(ParentPlanContext scope)
        {
            if (scope.OwnerDescriptor is { } owner)
            {
                if (!enclosingScopeStates.TryGetValue(owner, out var enclosingState))
                {
                    enclosingState = new ScopeState(scope.ParentSteps, scope.ParentInternalOperation);
                    enclosingScopeStates[owner] = enclosingState;
                }
                return enclosingState;
            }

            if (rootScopeState is null)
            {
                rootScopeState = new ScopeState(scope.ParentSteps, scope.ParentInternalOperation);
            }
            return rootScopeState;
        }

        foreach (var (downstreamStepId, downstreamStep) in downstreamByStepId)
        {
            foreach (var (_, requirement) in downstreamStep.Requirements)
            {
                // Requirements whose resolving path sits outside the
                // self-fetch's target are not our business; the self-fetch
                // shape is an existence proof that every requirement it
                // satisfies is reachable through its own target path.
                if (!selfFetch.Target.IsParentOfOrSame(requirement.Path))
                {
                    throw CreateUnsatisfiableDeferRequirementException(
                        selfFetch,
                        requirement,
                        "requirement path lies outside the defer's self-fetch target");
                }

                var resolved = false;
                var walkScope = parentContext;

                while (walkScope is not null)
                {
                    var scopeState = ScopeStateFor(walkScope);

                    // Same-subgraph attempt: inline the requirement's field
                    // selection into an existing parent-scope step that
                    // already targets the path on the same source schema.
                    if (TryInlineDeferRequirementInScope(
                        requirement,
                        selfFetch.SchemaName,
                        resolver,
                        scopeState,
                        out var parentStepId))
                    {
                        lifted.Add(new LiftedDeferRequirement(requirement, downstreamStepId, parentStepId));
                        resolved = true;
                        break;
                    }

                    // Cross-subgraph attempt: promote the sub-plan step that
                    // produces the requirement value into the enclosing scope
                    // as a dedicated op. Works for both root and enclosing
                    // defer scopes; the promoted op's output naturally merges
                    // into the scope's composite result document at runtime.
                    var (stepsAfterPromotion, newStepId, promotedSubPlanStepId) =
                        PlanCrossSubgraphDeferRequirement(
                            requirement,
                            downstreamStep,
                            subPlanSteps,
                            scopeState.Steps);

                    if (newStepId is { } resolvedStepId)
                    {
                        scopeState.Steps = stepsAfterPromotion;
                        lifted.Add(new LiftedDeferRequirement(requirement, downstreamStepId, resolvedStepId));

                        if (promotedSubPlanStepId is { } pid)
                        {
                            promotedSubPlanStepIds.Add(pid);
                        }

                        resolved = true;
                        break;
                    }

                    // This scope cannot serve the requirement. Escalate to
                    // the next enclosing scope and retry. At root exhaustion
                    // the loop exits with resolved=false.
                    walkScope = contextGraph.GetEnclosingScope(walkScope);
                }

                if (!resolved)
                {
                    throw CreateUnsatisfiableDeferRequirementException(
                        selfFetch,
                        requirement,
                        "no parent-reachable subgraph provides the required field");
                }
            }
        }

        if (lifted.Count == 0)
        {
            return subPlanSteps;
        }

        // Rewrite the sub-plan: drop the self-fetch and any promoted steps,
        // renumber the survivors to keep ids contiguous, and attach
        // ParentDependencies wiring so downstream steps source the value from
        // the parent scope.
        var rewrittenSubPlan = RewriteSubPlanAfterDeferRequirementRouting(
            subPlanSteps,
            selfFetch,
            lifted,
            promotedSubPlanStepIds);

        // Collect plan-scope requirements onto the descriptor so the runtime
        // can fetch variables from the parent result tree.
        foreach (var step in rewrittenSubPlan)
        {
            if (step is not OperationPlanStep operationStep)
            {
                continue;
            }

            foreach (var (key, requirement) in operationStep.Requirements)
            {
                descriptor.Requirements.TryAdd(key, requirement);
            }
        }

        // Push accumulated per-scope mutations back to the graph so that
        // subsequent siblings and nested descriptors observe them. The
        // internal operation update ensures the root operation's compile
        // (which runs after all sub-plans have routed) carries every field
        // absorbed for defer sub-plans.
        if (rootScopeState is not null)
        {
            contextGraph.UpdateRootSteps(rootScopeState.Steps);
            contextGraph.UpdateRootInternalOperation(rootScopeState.InternalOperation);
        }
        foreach (var (ownerDescriptor, state) in enclosingScopeStates)
        {
            contextGraph.UpdateDeferContext(ownerDescriptor, state.Steps, state.InternalOperation);
        }

        return rewrittenSubPlan;
    }

    /// <summary>
    /// Tries to inline the field selection implied by a defer's
    /// <paramref name="requirement"/> into a parent-scope step that already
    /// targets the requirement's path on the same source schema. Uses the
    /// planner's same helper as intra-plan field-requirement inlining so the
    /// operation document rewrite and selection-set index bookkeeping match.
    /// Mirrors the inline onto the parent's internal operation so the
    /// compiled parent Operation carries the field (the runtime composite
    /// result document relies on that field being preserved during result
    /// merging).
    /// </summary>
    private bool TryInlineDeferRequirementInScope(
        OperationRequirement requirement,
        string schemaName,
        ValueSelectionToSelectionSetRewriter resolver,
        ScopeState scopeState,
        out int parentStepId)
    {
        parentStepId = 0;

        for (var i = 0; i < scopeState.Steps.Count; i++)
        {
            if (scopeState.Steps[i] is not OperationPlanStep parentStep)
            {
                continue;
            }

            if (!string.Equals(parentStep.SchemaName, schemaName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!parentStep.Target.IsParentOfOrSame(requirement.Path))
            {
                continue;
            }

            if (!TryLocateDeferRequirementTarget(
                parentStep,
                requirement.Path,
                out _,
                out var targetType))
            {
                continue;
            }

            SelectionSetNode injectionSelections;
            try
            {
                injectionSelections = resolver.Rewrite(requirement.Map, targetType);
            }
            catch
            {
                continue;
            }

            var stepIndex = SelectionSetIndexer.Create(parentStep.Definition).ToBuilder();
            var targetId = stepIndex.GetId(
                LocateSelectionSetAtPath(parentStep.Definition.SelectionSet, requirement.Path, parentStep.Target.Length));

            var dependentsBeforeInline = parentStep.Dependents;

            if (!TryInlineSelectionSetIntoStep(
                parentStep,
                targetId,
                targetType,
                requirement.Path,
                injectionSelections,
                dependentStepId: 0,
                stepIndex,
                out var updatedParentStep,
                out _))
            {
                continue;
            }

            updatedParentStep = updatedParentStep with
            {
                Dependents = dependentsBeforeInline,
                SelectionSets = SelectionSetIndexer.CreateIdSet(updatedParentStep.Definition.SelectionSet, stepIndex)
            };

            scopeState.Steps = scopeState.Steps.SetItem(i, updatedParentStep);

            // Mirror the inline onto the enclosing scope's internal operation
            // so the compiled Operation (and the runtime composite result
            // document bound to it) also carries the selection.
            if (TryLocateInternalSelectionSetAtPath(
                scopeState.InternalOperation.SelectionSet,
                requirement.Path,
                out var internalTargetSelectionSet))
            {
                var overallIndex = SelectionSetIndexer.Create(scopeState.InternalOperation).ToBuilder();
                var overallTargetId = overallIndex.GetId(internalTargetSelectionSet);
                scopeState.InternalOperation = InlineSelectionsIntoOverallOperation(
                    scopeState.InternalOperation,
                    overallIndex,
                    targetType,
                    overallTargetId,
                    injectionSelections);
            }

            parentStepId = parentStep.Id;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Mutable per-scope state accumulated while routing a sub-plan's defer
    /// requirements. Each scope on the walker's path keeps its own step list
    /// and internal operation snapshot so that multiple requirements resolved
    /// at the same scope share their mutations and a single commit goes back
    /// to the context graph at the end of routing.
    /// </summary>
    private sealed class ScopeState(
        ImmutableList<PlanStep> steps,
        OperationDefinitionNode internalOperation)
    {
        public ImmutableList<PlanStep> Steps { get; set; } = steps;

        public OperationDefinitionNode InternalOperation { get; set; } = internalOperation;
    }

    private static bool TryLocateInternalSelectionSetAtPath(
        SelectionSetNode root,
        SelectionPath path,
        out SelectionSetNode targetSelectionSet)
    {
        var currentSet = root;

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Field:
                    FieldNode? fieldNode = null;
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is FieldNode candidate
                            && (candidate.Alias?.Value == segment.Name || candidate.Name.Value == segment.Name))
                        {
                            fieldNode = candidate;
                            break;
                        }
                    }

                    if (fieldNode?.SelectionSet is null)
                    {
                        targetSelectionSet = null!;
                        return false;
                    }

                    currentSet = fieldNode.SelectionSet;
                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    InlineFragmentNode? fragment = null;
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is InlineFragmentNode candidate
                            && candidate.TypeCondition?.Name.Value == segment.Name)
                        {
                            fragment = candidate;
                            break;
                        }
                    }

                    if (fragment is null)
                    {
                        targetSelectionSet = null!;
                        return false;
                    }

                    currentSet = fragment.SelectionSet;
                    break;

                default:
                    targetSelectionSet = null!;
                    return false;
            }
        }

        targetSelectionSet = currentSet;
        return true;
    }

    /// <summary>
    /// Handles a defer requirement that could not be inlined into any existing
    /// parent-scope step by promoting the sub-plan step that provides the
    /// required value into a dedicated parent-scope op. The promoted step
    /// keeps its original schema, lookup arguments and selection shape. Only
    /// its id gets renumbered into the parent scope and its sub-plan
    /// Dependents get cleared so downstream sub-plan steps source the value
    /// from <see cref="OperationPlanStep.ParentDependencies"/>.
    /// </summary>
    private (ImmutableList<PlanStep> UpdatedParentSteps, int? NewStepId, int? PromotedSubPlanStepId)
        PlanCrossSubgraphDeferRequirement(
            OperationRequirement requirement,
            OperationPlanStep consumingStep,
            ImmutableList<PlanStep> subPlanSteps,
            ImmutableList<PlanStep> parentSteps)
    {
        if (TryFindDeferRequirementProvider(subPlanSteps, consumingStep, requirement) is not { } providerStep)
        {
            return (parentSteps, null, null);
        }

        if (providerStep.SchemaName is null)
        {
            return (parentSteps, null, null);
        }

        var index = SelectionSetIndexer.Create(providerStep.Definition);
        var newParentStepId = parentSteps.NextId();
        var promotedStep = providerStep with
        {
            Id = newParentStepId,
            Dependents = ImmutableHashSet<int>.Empty,
            ParentDependencies = ImmutableHashSet<ParentStepRef>.Empty,
            SelectionSets = SelectionSetIndexer.CreateIdSet(providerStep.Definition.SelectionSet, index),
            RootSelectionSetId = index.GetId(providerStep.Definition.SelectionSet)
        };

        return (parentSteps.Add(promotedStep), newParentStepId, providerStep.Id);
    }

    /// <summary>
    /// Locates the sub-plan step that produces <paramref name="requirement"/>'s
    /// value for <paramref name="consumingStep"/>. The provider is a step the
    /// consuming step depends on whose target is a parent-of-or-same ancestor
    /// of the requirement path (so the provider's tree reaches the required
    /// entity) and whose selection set contains the requirement's mapped
    /// field.
    /// </summary>
    private static OperationPlanStep? TryFindDeferRequirementProvider(
        ImmutableList<PlanStep> subPlanSteps,
        OperationPlanStep consumingStep,
        OperationRequirement requirement)
    {
        var requirementFieldName = ExtractRootFieldName(requirement.Map.ToString());

        if (requirementFieldName is null)
        {
            return null;
        }

        foreach (var step in subPlanSteps)
        {
            if (step is not OperationPlanStep candidate
                || candidate.Id == consumingStep.Id
                || !candidate.Dependents.Contains(consumingStep.Id))
            {
                continue;
            }

            if (!candidate.Target.IsParentOfOrSame(requirement.Path))
            {
                continue;
            }

            if (SelectionSetContainsField(candidate.Definition.SelectionSet, requirementFieldName))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Drops the self-fetch and any promoted sub-plan steps, wires downstream
    /// dependencies onto their parent-scope providers, and renumbers surviving
    /// ids to stay contiguous.
    /// </summary>
    private static ImmutableList<PlanStep> RewriteSubPlanAfterDeferRequirementRouting(
        ImmutableList<PlanStep> subPlanSteps,
        OperationPlanStep selfFetch,
        List<LiftedDeferRequirement> lifted,
        HashSet<int> promotedSubPlanStepIds)
    {
        var parentRefsByStepId = new Dictionary<int, ImmutableHashSet<ParentStepRef>.Builder>();
        foreach (var entry in lifted)
        {
            if (!parentRefsByStepId.TryGetValue(entry.DownstreamStepId, out var builder))
            {
                builder = ImmutableHashSet.CreateBuilder<ParentStepRef>();
                parentRefsByStepId[entry.DownstreamStepId] = builder;
            }

            builder.Add(new ParentStepRef(entry.ParentStepId));
        }

        var droppedStepIds = new HashSet<int>(promotedSubPlanStepIds) { selfFetch.Id };
        var survivors = new List<PlanStep>(subPlanSteps.Count - droppedStepIds.Count);
        var oldToNewId = new Dictionary<int, int>(subPlanSteps.Count - droppedStepIds.Count);

        foreach (var step in subPlanSteps)
        {
            if (droppedStepIds.Contains(step.Id))
            {
                continue;
            }

            oldToNewId[step.Id] = survivors.Count + 1;
            survivors.Add(step);
        }

        var rewritten = ImmutableList.CreateBuilder<PlanStep>();

        foreach (var step in survivors)
        {
            if (step is OperationPlanStep operationStep)
            {
                var newDependents = RenumberDeferDependents(operationStep.Dependents, droppedStepIds, oldToNewId);
                var newParentDependencies = operationStep.ParentDependencies;

                if (parentRefsByStepId.TryGetValue(operationStep.Id, out var parentRefBuilder))
                {
                    newParentDependencies = newParentDependencies.Union(parentRefBuilder.ToImmutable());
                }

                rewritten.Add(operationStep with
                {
                    Id = oldToNewId[operationStep.Id],
                    Dependents = newDependents,
                    ParentDependencies = newParentDependencies
                });
            }
            else
            {
                rewritten.Add(step with { Id = oldToNewId[step.Id] });
            }
        }

        return rewritten.ToImmutable();
    }

    private static ImmutableHashSet<int> RenumberDeferDependents(
        ImmutableHashSet<int> dependents,
        HashSet<int> removedStepIds,
        Dictionary<int, int> oldToNewId)
    {
        if (dependents.IsEmpty)
        {
            return dependents;
        }

        var builder = ImmutableHashSet.CreateBuilder<int>();
        foreach (var dependentId in dependents)
        {
            if (removedStepIds.Contains(dependentId))
            {
                continue;
            }

            if (oldToNewId.TryGetValue(dependentId, out var newId))
            {
                builder.Add(newId);
            }
        }

        return builder.ToImmutable();
    }

    /// <summary>
    /// Walks <paramref name="parentStep"/>'s definition to locate the selection
    /// set at <paramref name="path"/> and resolves its GraphQL type.
    /// </summary>
    private bool TryLocateDeferRequirementTarget(
        OperationPlanStep parentStep,
        SelectionPath path,
        out SelectionSetNode targetSelectionSet,
        out ITypeDefinition targetType)
    {
        var currentSet = parentStep.Definition.SelectionSet;
        ITypeDefinition currentType = _schema.GetOperationType(parentStep.Definition.Operation);

        var startIndex = parentStep.Target.Length;

        for (var i = startIndex; i < path.Length; i++)
        {
            var segment = path[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Field:
                    FieldNode? fieldNode = null;
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is FieldNode candidate
                            && (candidate.Alias?.Value == segment.Name || candidate.Name.Value == segment.Name))
                        {
                            fieldNode = candidate;
                            break;
                        }
                    }

                    if (fieldNode?.SelectionSet is null
                        || currentType is not FusionComplexTypeDefinition complexType
                        || !complexType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            allowInaccessibleFields: true,
                            out var field))
                    {
                        targetSelectionSet = null!;
                        targetType = null!;
                        return false;
                    }

                    currentSet = fieldNode.SelectionSet;
                    currentType = field.Type.NamedType();
                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    InlineFragmentNode? fragment = null;
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is InlineFragmentNode candidate
                            && candidate.TypeCondition?.Name.Value == segment.Name)
                        {
                            fragment = candidate;
                            break;
                        }
                    }

                    if (fragment is null
                        || !_schema.Types.TryGetType(segment.Name, out var fragmentType))
                    {
                        targetSelectionSet = null!;
                        targetType = null!;
                        return false;
                    }

                    currentSet = fragment.SelectionSet;
                    currentType = fragmentType;
                    break;

                default:
                    targetSelectionSet = null!;
                    targetType = null!;
                    return false;
            }
        }

        targetSelectionSet = currentSet;
        targetType = currentType;
        return true;
    }

    private static SelectionSetNode LocateSelectionSetAtPath(
        SelectionSetNode root,
        SelectionPath path,
        int startIndex)
    {
        var currentSet = root;

        for (var i = startIndex; i < path.Length; i++)
        {
            var segment = path[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Field:
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is FieldNode candidate
                            && (candidate.Alias?.Value == segment.Name || candidate.Name.Value == segment.Name)
                            && candidate.SelectionSet is { } inner)
                        {
                            currentSet = inner;
                            break;
                        }
                    }
                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    foreach (var selection in currentSet.Selections)
                    {
                        if (selection is InlineFragmentNode candidate
                            && candidate.TypeCondition?.Name.Value == segment.Name)
                        {
                            currentSet = candidate.SelectionSet;
                            break;
                        }
                    }
                    break;
            }
        }

        return currentSet;
    }

    private static bool SelectionSetContainsField(
        SelectionSetNode selectionSet,
        string fieldName)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field
                    when (field.Alias?.Value ?? field.Name.Value).Equals(fieldName, StringComparison.Ordinal):
                    return true;

                case FieldNode { SelectionSet: { } nested }:
                    if (SelectionSetContainsField(nested, fieldName))
                    {
                        return true;
                    }
                    break;

                case InlineFragmentNode fragment:
                    if (SelectionSetContainsField(fragment.SelectionSet, fieldName))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static string? ExtractRootFieldName(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];

            if (c == ' ' || c == '{' || c == '.')
            {
                return i == 0 ? null : text[..i];
            }
        }

        return text;
    }

    private static InvalidOperationException CreateUnsatisfiableDeferRequirementException(
        OperationPlanStep selfFetch,
        OperationRequirement requirement,
        string reason)
        => new(
            $"The deferred sub-plan's requirement '{requirement.Key}' at path "
            + $"'{requirement.Path}' could not be resolved from the parent plan. "
            + $"Defer anchor: '{selfFetch.Target}'. "
            + $"Target schema: '{selfFetch.SchemaName}'. "
            + $"Reason: {reason}.");

    private readonly record struct LiftedDeferRequirement(
        OperationRequirement Requirement,
        int DownstreamStepId,
        int ParentStepId);

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

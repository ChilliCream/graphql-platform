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
    /// Plans each <see cref="IncrementalPlanDescriptor"/> and records any
    /// parent-scope requirements needed by its incremental plan.
    /// </summary>
    private ImmutableArray<DeferRoutingState> RouteIncrementalPlans(
        string id,
        DeferSplitResult splitResult,
        PlanContextGraph contextGraph,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        if (splitResult.IncrementalPlanDescriptors.IsEmpty)
        {
            return [];
        }

        var routingStates = ImmutableArray.CreateBuilder<DeferRoutingState>(
            splitResult.IncrementalPlanDescriptors.Length);

        for (var i = 0; i < splitResult.IncrementalPlanDescriptors.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var descriptor = splitResult.IncrementalPlanDescriptors[i];

            var parentContext = contextGraph.GetParentContext(descriptor);
            var incrementalPlanResult = PlanIncrementalPlan(
                id,
                descriptor,
                i,
                emitPlannerEvents,
                cancellationToken);

            var rewrittenIncrementalPlan = ApplyDeferRequirementsToParent(
                descriptor,
                incrementalPlanResult.Steps,
                parentContext,
                contextGraph);

            var registeredInternalOp = incrementalPlanResult.InternalOperationDefinition ?? descriptor.Operation;
            contextGraph.RegisterDeferContext(
                descriptor,
                rewrittenIncrementalPlan,
                SelectionSetIndexer.Create(registeredInternalOp),
                registeredInternalOp);

            routingStates.Add(new DeferRoutingState(descriptor, i));
        }

        return routingStates.ToImmutable();
    }

    /// <summary>
    /// Builds the incremental plans for the routed descriptors.
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

        var incrementalPlansBuilder = ImmutableArray.CreateBuilder<IncrementalPlan>(routingStates.Length);

        foreach (var routingState in routingStates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var descriptor = routingState.Descriptor;

            // Use the registered scope state because nested descriptors may
            // have added parent-scope requirements.
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

            var incrementalPlan = new IncrementalPlan(
                deferredOperation,
                rootNodes,
                allNodes,
                descriptor.DeliveryGroupSet,
                planScopeRequirements);

            incrementalPlansBuilder.Add(incrementalPlan);
        }

        return incrementalPlansBuilder.ToImmutable();
    }

    /// <summary>
    /// Captures the routed descriptor order used when building incremental plans.
    /// </summary>
    private readonly record struct DeferRoutingState(
        IncrementalPlanDescriptor Descriptor,
        int Index);

    /// <summary>
    /// Plans a single incremental plan descriptor.
    /// </summary>
    private DeferIncrementalPlanResult PlanIncrementalPlan(
        string operationId,
        IncrementalPlanDescriptor descriptor,
        int incrementalPlanId,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        var deferredOperation = descriptor.Operation;

        var index = SelectionSetIndexer.Create(deferredOperation);

        var (node, selectionSet) = CreateQueryPlanBase(deferredOperation, "defer", index);

        if (node.Backlog.IsEmpty)
        {
            return new DeferIncrementalPlanResult([], null);
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

        var plan = Plan(operationId + "#defer_" + incrementalPlanId, possiblePlans, emitPlannerEvents, cancellationToken);

        if (!plan.HasValue)
        {
            return new DeferIncrementalPlanResult([], null);
        }

        return new DeferIncrementalPlanResult(
            plan.Value.Steps,
            plan.Value.InternalOperationDefinition);
    }

    /// <summary>
    /// Moves requirements that can be satisfied by the enclosing plan scope out
    /// of the incremental plan and records them as parent-scope dependencies.
    /// </summary>
    /// <returns>
    /// The updated incremental plan step list, or the original list when no
    /// parent-scope requirements can be moved.
    /// </returns>
    private ImmutableList<PlanStep> ApplyDeferRequirementsToParent(
        IncrementalPlanDescriptor descriptor,
        ImmutableList<PlanStep> incrementalPlanSteps,
        ParentPlanContext parentContext,
        PlanContextGraph contextGraph)
    {
        // Parent-scope requirements can only be moved when an initial step has
        // dependent steps.
        if (incrementalPlanSteps.Count < 2
            || incrementalPlanSteps[0] is not OperationPlanStep selfFetch
            || selfFetch.SchemaName is null
            || selfFetch.Dependents.IsEmpty)
        {
            return incrementalPlanSteps;
        }

        // Collect steps that depend directly on the initial requirement step.
        var downstreamByStepId = new Dictionary<int, OperationPlanStep>();
        foreach (var dependentStepId in selfFetch.Dependents)
        {
            if (incrementalPlanSteps.ById(dependentStepId) is OperationPlanStep dependentStep)
            {
                downstreamByStepId[dependentStepId] = dependentStep;
            }
        }

        if (downstreamByStepId.Count == 0)
        {
            return incrementalPlanSteps;
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
        var promotedIncrementalPlanStepIds = new HashSet<int>();

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

                    // When no existing parent-scope step can supply the
                    // requirement, add a dedicated step to the enclosing scope.
                    var (stepsAfterPromotion, newStepId, promotedIncrementalPlanStepId) =
                        PlanCrossSubgraphDeferRequirement(
                            requirement,
                            downstreamStep,
                            incrementalPlanSteps,
                            scopeState.Steps);

                    if (newStepId is { } resolvedStepId)
                    {
                        scopeState.Steps = stepsAfterPromotion;
                        lifted.Add(new LiftedDeferRequirement(requirement, downstreamStepId, resolvedStepId));

                        if (promotedIncrementalPlanStepId is { } pid)
                        {
                            promotedIncrementalPlanStepIds.Add(pid);
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
            return incrementalPlanSteps;
        }

        // Remove moved steps from the incremental plan and record parent-scope
        // dependencies on the remaining steps.
        var rewrittenIncrementalPlan = RewriteIncrementalPlanAfterDeferRequirementRouting(
            incrementalPlanSteps,
            selfFetch,
            lifted,
            promotedIncrementalPlanStepIds);

        // Record the parent-scope requirements on the descriptor.
        foreach (var step in rewrittenIncrementalPlan)
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

        // Publish scope updates before processing additional descriptors.
        if (rootScopeState is not null)
        {
            contextGraph.UpdateRootSteps(rootScopeState.Steps);
            contextGraph.UpdateRootInternalOperation(rootScopeState.InternalOperation);
        }
        foreach (var (ownerDescriptor, state) in enclosingScopeStates)
        {
            contextGraph.UpdateDeferContext(ownerDescriptor, state.Steps, state.InternalOperation);
        }

        return rewrittenIncrementalPlan;
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
    /// Mutable planning state for an enclosing scope while parent-scope
    /// requirements are resolved.
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
    /// Creates a parent-scope step for a requirement that cannot be satisfied
    /// by an existing parent-scope step.
    /// </summary>
    private (ImmutableList<PlanStep> UpdatedParentSteps, int? NewStepId, int? PromotedIncrementalPlanStepId)
        PlanCrossSubgraphDeferRequirement(
            OperationRequirement requirement,
            OperationPlanStep consumingStep,
            ImmutableList<PlanStep> incrementalPlanSteps,
            ImmutableList<PlanStep> parentSteps)
    {
        if (TryFindDeferRequirementProvider(incrementalPlanSteps, consumingStep, requirement) is not { } providerStep)
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
    /// Locates the incremental plan step that produces <paramref name="requirement"/>'s
    /// value for <paramref name="consumingStep"/>. The provider is a step the
    /// consuming step depends on whose target is a parent-of-or-same ancestor
    /// of the requirement path (so the provider's tree reaches the required
    /// entity) and whose selection set contains the requirement's mapped
    /// field.
    /// </summary>
    private static OperationPlanStep? TryFindDeferRequirementProvider(
        ImmutableList<PlanStep> incrementalPlanSteps,
        OperationPlanStep consumingStep,
        OperationRequirement requirement)
    {
        var requirementFieldName = ExtractRootFieldName(requirement.Map.ToString());

        if (requirementFieldName is null)
        {
            return null;
        }

        foreach (var step in incrementalPlanSteps)
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
    /// Removes steps that moved to the parent scope and records parent
    /// dependencies on the remaining steps.
    /// </summary>
    private static ImmutableList<PlanStep> RewriteIncrementalPlanAfterDeferRequirementRouting(
        ImmutableList<PlanStep> incrementalPlanSteps,
        OperationPlanStep selfFetch,
        List<LiftedDeferRequirement> lifted,
        HashSet<int> promotedIncrementalPlanStepIds)
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

        var droppedStepIds = new HashSet<int>(promotedIncrementalPlanStepIds) { selfFetch.Id };
        var survivors = new List<PlanStep>(incrementalPlanSteps.Count - droppedStepIds.Count);
        var oldToNewId = new Dictionary<int, int>(incrementalPlanSteps.Count - droppedStepIds.Count);

        foreach (var step in incrementalPlanSteps)
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
            $"The deferred incremental plan's requirement '{requirement.Key}' at path "
            + $"'{requirement.Path}' could not be resolved from the parent plan. "
            + $"Defer anchor: '{selfFetch.Target}'. "
            + $"Target schema: '{selfFetch.SchemaName}'. "
            + $"Reason: {reason}.");

    private readonly record struct LiftedDeferRequirement(
        OperationRequirement Requirement,
        int DownstreamStepId,
        int ParentStepId);

    /// <summary>
    /// Builds execution nodes for an incremental plan's plan steps.
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

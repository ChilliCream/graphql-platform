using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Rewriters;
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
        string shortHash,
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
                finalSteps,
                finalSteps.NextId());

            var compiledOp = AddTypeNameToAbstractSelections(
                registeredInternalOp,
                _schema.GetOperationType(registeredInternalOp.Operation));
            var deferredOperation = _operationCompiler.Compile(
                id + "#defer_" + routingState.Index,
                hash + "#defer_" + routingState.Index,
                shortHash,
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

        var possiblePlans = new PlanQueue(_schema);
        PlanNode node;

        if (descriptor.Path.IsRoot)
        {
            // A defer anchored at the operation root has nothing to key a lookup off of;
            // it is planned like a normal root-rooted operation.
            SelectionSet selectionSet;
            (node, selectionSet) = CreateQueryPlanBase(deferredOperation, "defer", index);

            if (node.Backlog.IsEmpty)
            {
                return new DeferIncrementalPlanResult([], null);
            }

            foreach (var (schemaName, resolutionCost) in _schema.GetPossibleSchemas(selectionSet))
            {
                possiblePlans.Enqueue(
                    node with
                    {
                        SchemaName = schemaName,
                        ResolutionCost = resolutionCost
                    });
            }
        }
        else
        {
            // A defer anchored directly on the root's `node(id: ...)` field cannot be
            // planned as an ordinary keyed lookup: `Node` is not an entity with its own
            // lookup, its concrete type is only known once the id is decoded, and that
            // decoding, per-concrete-type schema resolution, and shared-selection
            // handling all live in the dedicated NodeFieldWorkItem path that
            // CreateQueryPlanBase also uses for a root-anchored `node` selection. Reuse
            // that same path here instead of seeding a Lookup work item for the `Node`
            // interface, which the ordinary lookup machinery cannot serve.
            Backlog backlog;

            if (TryLocateNodeFieldAnchor(deferredOperation, descriptor.Path, out var nodeFieldNode))
            {
                backlog = Backlog.Empty.Push(
                    new NodeFieldWorkItem(new NodeField { Field = nodeFieldNode, ParentFragments = null }));
            }
            else
            {
                // A defer anchored inside a parent result (a list item, an object) is
                // planned as a keyed lookup rooted at the anchor's own type rather than a
                // fresh operation re-rooted at Query. Seeding a Lookup work item (instead
                // of a Root work item) lets PlanQueue.EnqueueLookupPlanNodes pick a direct
                // lookup for the anchor type when one exists; when none exists it falls
                // back to walking the path up to the root
                // (EnqueueParentPathLookupPlanNodes), which reproduces the previous root
                // re-fetch behavior.
                if (!TryLocateIncrementalPlanAnchor(
                    deferredOperation,
                    descriptor.Path,
                    out var anchorSelectionSet,
                    out var anchorType))
                {
                    throw new InvalidOperationException(
                        $"Unable to locate the defer anchor selection set at path '{descriptor.Path}' "
                        + "inside the incremental plan operation.");
                }

                var anchorSet = new SelectionSet(
                    index.GetId(anchorSelectionSet),
                    anchorSelectionSet,
                    anchorType,
                    descriptor.Path);

                backlog = Backlog.Empty.Push(new OperationWorkItem(OperationWorkItemKind.Lookup, anchorSet));
            }

            var remainingCost = PlannerCostEstimator.EstimateRemainingCost(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlog.Cost);

            node = new PlanNode
            {
                OperationDefinition = deferredOperation,
                InternalOperationDefinition = deferredOperation,
                ShortHash = "defer",
                SchemaName = Planning.PlanNode.UnresolvedSchemaName,
                Options = _options,
                SelectionSetIndex = index,
                Backlog = backlog,
                RemainingCost = remainingCost,
                OperationStepCount = 0
            };

            possiblePlans.EnqueueBranches(node);
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
    /// Walks <paramref name="operation"/>'s selection set from its root type to locate
    /// the selection set at <paramref name="path"/> and resolves its GraphQL type. Used
    /// to find the anchor selection set for a non-root-anchored incremental plan, whose
    /// operation is a full path replica from <c>Query</c> down to the deferred fields.
    /// </summary>
    private bool TryLocateIncrementalPlanAnchor(
        OperationDefinitionNode operation,
        SelectionPath path,
        out SelectionSetNode anchorSelectionSet,
        out ITypeDefinition anchorType)
        => TryLocateSelectionSetAtPath(
            operation.SelectionSet,
            _schema.GetOperationType(operation.Operation),
            path,
            startIndex: 0,
            out anchorSelectionSet,
            out anchorType);

    /// <summary>
    /// Determines whether <paramref name="path"/> is exactly the root's own
    /// <c>node(id: ...)</c> field (Global Object Identification's <c>Node</c> interface
    /// field, which only ever appears directly on <c>Query</c>) and, if so, locates that
    /// field within <paramref name="operation"/>.
    /// </summary>
    private bool TryLocateNodeFieldAnchor(
        OperationDefinitionNode operation,
        SelectionPath path,
        out FieldNode nodeField)
    {
        nodeField = null!;

        if (path.Length != 1
            || path[0].Kind != SelectionPathSegmentKind.Field
            || !_schema.QueryType.Fields.TryGetField(
                path[0].Name,
                allowInaccessibleFields: true,
                out var field)
            || field is not { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } })
        {
            return false;
        }

        foreach (var selection in operation.SelectionSet.Selections)
        {
            if (selection is FieldNode candidate
                && (candidate.Alias?.Value == path[0].Name || candidate.Name.Value == path[0].Name))
            {
                nodeField = candidate;
                return true;
            }
        }

        return false;
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
        if (incrementalPlanSteps.Count < 2)
        {
            return incrementalPlanSteps;
        }

        // A producer step exists purely to supply another incremental-plan step's
        // requirement: it targets the defer's own anchor (the same entity the
        // enclosing scope already fetches down to) and every field it selects is
        // consumed as a requirement by its dependents, so it never carries
        // client-visible deferred output of its own. Such a step's data can always
        // be routed to the enclosing scope instead of being fetched a second time
        // inside this incremental plan. Earlier seeding always produced exactly one
        // such step at index 0; a lookup-anchored seed may produce it anywhere in
        // the list (or several, chained through each other), so every step is
        // considered.
        var producers = FindDeferRequirementProducers(incrementalPlanSteps, descriptor.Path);

        if (producers.IsEmpty)
        {
            return incrementalPlanSteps;
        }

        var producerIds = new HashSet<int>();
        foreach (var producer in producers)
        {
            producerIds.Add(producer.Id);
        }

        // Collect every step that depends directly on a producer; that is exactly
        // the set of steps whose requirements need to be routed to the enclosing
        // scope.
        var downstreamByStepId = new Dictionary<int, OperationPlanStep>();
        foreach (var producer in producers)
        {
            foreach (var dependentStepId in producer.Dependents)
            {
                if (incrementalPlanSteps.ById(dependentStepId) is OperationPlanStep dependentStep)
                {
                    downstreamByStepId[dependentStepId] = dependentStep;
                }
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

            rootScopeState ??= new ScopeState(scope.ParentSteps, scope.ParentInternalOperation);
            return rootScopeState;
        }

        foreach (var (downstreamStepId, downstreamStep) in downstreamByStepId)
        {
            foreach (var (_, requirement) in downstreamStep.Requirements)
            {
                // A requirement must sit at or above the defer's own anchor: at the
                // anchor itself (the common case), or at an ancestor of it (a nested
                // defer bootstrapping a key at the enclosing defer's own anchor, several
                // path segments above its own, to reach its deeper one). A requirement
                // anchored below or beside the defer's own path would mean a producer
                // reaches somewhere this defer's own tree does not, which should not
                // happen given every producer's target is at or above this same path.
                if (!requirement.Path.IsParentOfOrSame(descriptor.Path))
                {
                    throw CreateUnsatisfiableDeferRequirementException(
                        producers[0],
                        requirement,
                        "requirement path lies outside the defer's anchor");
                }

                // See through a chain of producers that do nothing but relay this same
                // field from one subgraph to the next (a key value bootstrapped across
                // several subgraphs before the entity that actually needs the deferred
                // field is reached) to the step that truly grounds it, so both the
                // same-subgraph inline attempt and a cross-subgraph promotion target
                // the real origin instead of an intermediate pass-through hop.
                var (sourceStep, sourceRequirement) = ResolveDeferRequirementSource(
                    incrementalPlanSteps,
                    downstreamStep,
                    requirement,
                    producerIds);
                var schemaHint =
                    (sourceStep is not null
                        ? TryFindDeferRequirementProvider(incrementalPlanSteps, sourceStep, sourceRequirement)?.SchemaName
                        : null)
                    ?? producers[0].SchemaName!;

                var resolved = false;
                var walkScope = parentContext;

                while (walkScope is not null)
                {
                    var scopeState = ScopeStateFor(walkScope);

                    // Same-subgraph attempt: inline the requirement's field
                    // selection into an existing parent-scope step that
                    // already targets the path on the same source schema.
                    if (TryInlineDeferRequirementInScope(
                        sourceRequirement,
                        schemaHint,
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
                            sourceRequirement,
                            sourceStep ?? downstreamStep,
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
                        producers[0],
                        requirement,
                        "no parent-reachable subgraph provides the required field");
                }
            }
        }

        if (lifted.Count == 0)
        {
            return incrementalPlanSteps;
        }

        // Drop every producer step (its data now comes from the enclosing scope)
        // along with any incremental-plan step that got promoted wholesale into
        // the enclosing scope, and record parent-scope dependencies on the
        // remaining steps.
        var droppedStepIds = new HashSet<int>(promotedIncrementalPlanStepIds);
        foreach (var producer in producers)
        {
            droppedStepIds.Add(producer.Id);
        }

        var rewrittenIncrementalPlan = RewriteIncrementalPlanAfterDeferRequirementRouting(
            incrementalPlanSteps,
            lifted,
            droppedStepIds);

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
                LocateSelectionSetAtPath(GetStepEntitySelectionSet(parentStep), requirement.Path, parentStep.Target.Length));

            var dependentsBeforeInline = parentStep.Dependents;

            if (!TryInlineSelectionSetIntoStep(
                parentStep,
                targetId,
                targetType,
                requirement.Path,
                injectionSelections,
                dependentStepId: 0,
                stepIndex,
                new RequirementAliasContext([], RequirementAliasRegistry.Empty),
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
            Dependents = [],
            ParentDependencies = [],
            SelectionSets = SelectionSetIndexer.CreateIdSet(providerStep.Definition.SelectionSet, index),
            RootSelectionSetId = index.GetId(providerStep.Definition.SelectionSet)
        };

        return (parentSteps.Add(promotedStep), newParentStepId, providerStep.Id);
    }

    /// <summary>
    /// Resolves the schema a same-subgraph inline attempt should try first for
    /// <paramref name="requirement"/>, owed by <paramref name="downstreamStep"/>. When the
    /// in-plan provider is itself a producer being dropped and does nothing but relay the
    /// very same field it required upstream (a key value bootstrapped through several
    /// subgraphs before the entity that actually needs the deferred field is reached,
    /// each hop just re-fetching the same field under a different schema), the walk
    /// continues past it to whatever ultimately grounds that field, so the enclosing
    /// scope is asked for the true origin instead of promoting a pass-through hop. A
    /// producer that transforms its input into a different field (an "id" argument
    /// producing "productSku", say) is the true, final provider of that field and is
    /// never skipped past.
    /// </summary>
    private static (OperationPlanStep? ConsumingStep, OperationRequirement Requirement) ResolveDeferRequirementSource(
        ImmutableList<PlanStep> incrementalPlanSteps,
        OperationPlanStep downstreamStep,
        OperationRequirement requirement,
        HashSet<int> producerIds)
    {
        var current = downstreamStep;
        var currentRequirement = requirement;
        var visited = new HashSet<int>();

        while (true)
        {
            if (TryFindDeferRequirementProvider(incrementalPlanSteps, current, currentRequirement)
                is not { } provider)
            {
                return (null, currentRequirement);
            }

            if (!producerIds.Contains(provider.Id)
                || provider.Requirements.Count != 1
                || !visited.Add(provider.Id))
            {
                return (current, currentRequirement);
            }

            var neededFieldName =
                currentRequirement.InternalAlias ?? ExtractRootFieldName(currentRequirement.Map.ToString());
            var upstreamRequirement = provider.Requirements.Values.Single();
            var upstreamFieldName =
                upstreamRequirement.InternalAlias ?? ExtractRootFieldName(upstreamRequirement.Map.ToString());

            if (neededFieldName is null
                || upstreamFieldName is null
                || !neededFieldName.Equals(upstreamFieldName, StringComparison.Ordinal))
            {
                return (current, currentRequirement);
            }

            current = provider;
            currentRequirement = upstreamRequirement;
        }
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
        var requirementFieldName =
            requirement.InternalAlias
                ?? ExtractRootFieldName(requirement.Map.ToString());

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
    /// Finds every step in <paramref name="incrementalPlanSteps"/> that exists purely to
    /// supply another incremental-plan step's requirement: it targets either the defer's
    /// own <paramref name="anchorPath"/> (the entity the enclosing scope already fetches
    /// down to) or the operation root (a bootstrap key value reconstructed by replaying
    /// the client's own root-field access), and every field it selects is consumed as a
    /// requirement by its dependents, so dropping it and routing its data through the
    /// enclosing scope loses no client-visible output. A key value can be bootstrapped
    /// through a short chain of such steps (each re-fetching the key on a different
    /// subgraph before the entity that actually needs the deferred field is reached), so
    /// every step is considered, not just the first.
    /// </summary>
    private static ImmutableArray<OperationPlanStep> FindDeferRequirementProducers(
        ImmutableList<PlanStep> incrementalPlanSteps,
        SelectionPath anchorPath)
    {
        var producers = ImmutableArray.CreateBuilder<OperationPlanStep>();

        foreach (var step in incrementalPlanSteps)
        {
            if (step is OperationPlanStep operationStep
                && operationStep.SchemaName is not null
                && !operationStep.Dependents.IsEmpty
                && (operationStep.Target.Equals(anchorPath) || operationStep.Target.IsRoot)
                && IsPureRequirementProducer(operationStep, incrementalPlanSteps))
            {
                producers.Add(operationStep);
            }
        }

        return producers.ToImmutable();
    }

    /// <summary>
    /// Determines whether every field <paramref name="step"/> selects is consumed as a
    /// requirement by one of its dependents, meaning the step carries no output of its
    /// own beyond what those dependents need from it.
    /// </summary>
    private static bool IsPureRequirementProducer(
        OperationPlanStep step,
        ImmutableList<PlanStep> incrementalPlanSteps)
    {
        var ownFields = new HashSet<string>(StringComparer.Ordinal);
        CollectLeafFieldNames(step.Definition.SelectionSet, ownFields);

        if (ownFields.Count == 0)
        {
            return false;
        }

        var consumed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var dependentStepId in step.Dependents)
        {
            if (incrementalPlanSteps.ById(dependentStepId) is not OperationPlanStep dependentStep)
            {
                continue;
            }

            foreach (var (_, requirement) in dependentStep.Requirements)
            {
                var fieldName = requirement.InternalAlias ?? ExtractRootFieldName(requirement.Map.ToString());

                if (fieldName is not null)
                {
                    consumed.Add(fieldName);
                }
            }
        }

        foreach (var fieldName in ownFields)
        {
            if (!consumed.Contains(fieldName))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Collects the response names of every leaf field selection within
    /// <paramref name="selectionSet"/>, descending through composite fields and inline
    /// fragments (a composite field wraps a lookup or entity boundary, not output of its
    /// own). The <c>__typename</c> discriminator is ignored; it never satisfies a
    /// requirement.
    /// </summary>
    private static void CollectLeafFieldNames(SelectionSetNode selectionSet, HashSet<string> names)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode { SelectionSet: { } nested }:
                    CollectLeafFieldNames(nested, names);
                    break;

                case FieldNode field:
                    if (!field.Name.Value.Equals("__typename", StringComparison.Ordinal))
                    {
                        names.Add(field.Alias?.Value ?? field.Name.Value);
                    }
                    break;

                case InlineFragmentNode fragment:
                    CollectLeafFieldNames(fragment.SelectionSet, names);
                    break;
            }
        }
    }

    /// <summary>
    /// Removes steps that moved to the parent scope and records parent
    /// dependencies on the remaining steps.
    /// </summary>
    private static ImmutableList<PlanStep> RewriteIncrementalPlanAfterDeferRequirementRouting(
        ImmutableList<PlanStep> incrementalPlanSteps,
        List<LiftedDeferRequirement> lifted,
        HashSet<int> droppedStepIds)
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
    /// Returns the selection set that holds <paramref name="step"/>'s own entity-level
    /// fields. A keyed-lookup step's <see cref="OperationPlanStep.Definition"/> wraps
    /// those fields inside a synthetic lookup field (for example
    /// <c>userById(id: $x) { ... }</c>) that has no corresponding <see cref="SelectionPath"/>
    /// segment; the entity's own fields start one level inside that wrapper. A step with
    /// no lookup (a root fetch, or a step reusing the client's own path) has no such
    /// wrapper, so its definition's top-level selection set already sits at
    /// <see cref="OperationPlanStep.Target"/>.
    /// </summary>
    private static SelectionSetNode GetStepEntitySelectionSet(OperationPlanStep step)
        => step.Lookup is not null
            && step.Definition.SelectionSet.Selections is [FieldNode { SelectionSet: { } lookupSelectionSet }]
                ? lookupSelectionSet
                : step.Definition.SelectionSet;

    /// <summary>
    /// Returns the GraphQL type of <paramref name="step"/>'s own entity-level selection
    /// set (see <see cref="GetStepEntitySelectionSet"/>).
    /// </summary>
    private ITypeDefinition GetStepEntityType(OperationPlanStep step)
        => step.Lookup is not null
            && step.Definition.SelectionSet.Selections is [FieldNode { SelectionSet: not null }]
                ? step.Type
                : _schema.GetOperationType(step.Definition.Operation);

    /// <summary>
    /// Walks <paramref name="parentStep"/>'s definition to locate the selection
    /// set at <paramref name="path"/> and resolves its GraphQL type.
    /// </summary>
    private bool TryLocateDeferRequirementTarget(
        OperationPlanStep parentStep,
        SelectionPath path,
        out SelectionSetNode targetSelectionSet,
        out ITypeDefinition targetType)
        => TryLocateSelectionSetAtPath(
            GetStepEntitySelectionSet(parentStep),
            GetStepEntityType(parentStep),
            path,
            startIndex: parentStep.Target.Length,
            out targetSelectionSet,
            out targetType);

    /// <summary>
    /// Walks <paramref name="startSet"/> from <paramref name="path"/>'s <paramref name="startIndex"/>
    /// segment onward, following fields and inline fragments, to locate the selection set at
    /// <paramref name="path"/> and resolve its GraphQL type.
    /// </summary>
    private bool TryLocateSelectionSetAtPath(
        SelectionSetNode startSet,
        ITypeDefinition startType,
        SelectionPath path,
        int startIndex,
        out SelectionSetNode targetSelectionSet,
        out ITypeDefinition targetType)
    {
        var currentSet = startSet;
        var currentType = startType;

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
                        || !_schema.Types.TryGetType(
                            segment.Name,
                            allowInaccessibleFields: true,
                            out var fragmentType))
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
        ImmutableList<PlanStep> planSteps,
        int nextNodeId)
    {
        if (planSteps.Count == 0)
        {
            return ([], []);
        }

        var ctx = new ExecutionPlanBuildContext(nextNodeId);
        var hasVariables = deferredOperation.VariableDefinitions.Count > 0;

        planSteps = TransformPlanSteps(planSteps, deferredOperation);
        IndexDependencies(planSteps, ctx);
        BuildExecutionNodes(planSteps, ctx, _schema, hasVariables, CancellationToken.None);
        MergeAndBatchOperations(ctx, _options.EnableRequestGrouping, _options.MergePolicy, _schema);
        WireExecutionDependencies(ctx);

        var rootNodes = planSteps
            .Select(t => ResolveRedirectedStepId(t.Id, ctx.RedirectedStepIds))
            .Distinct()
            .Where(id => !ctx.DependenciesByStepId.ContainsKey(id) && ctx.ExecutionNodes.ContainsKey(id))
            .Select(id => ctx.ExecutionNodes[id])
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

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Converters;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Fusion.Types.Rewriters;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using ArgumentNode = HotChocolate.Language.ArgumentNode;
using NameNode = HotChocolate.Language.NameNode;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private readonly FusionSchemaDefinition _schema;
    private readonly OperationCompiler _operationCompiler;
    private readonly MergeSelectionSetRewriter _mergeRewriter;
    private readonly SelectionSetPartitioner _partitioner;
    private readonly SelectionSetByTypePartitioner _selectionSetByTypePartitioner;
    private readonly NodeFieldSelectionSetPartitioner _nodeFieldSelectionSetPartitioner;
    private readonly SourceSchemaNodeCandidateResolver _sourceSchemaNodeCandidateResolver;
    private readonly OperationPlannerOptions _options;
    private PolicyPlanningState _policyState;
    private bool? _schemaHasDivergentInterfaceFields;

    public OperationPlanner(
        FusionSchemaDefinition schema,
        OperationCompiler operationCompiler)
        : this(schema, operationCompiler, OperationPlannerOptions.Default)
    {
    }

    public OperationPlanner(
        FusionSchemaDefinition schema,
        OperationCompiler operationCompiler,
        OperationPlannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operationCompiler);
        ArgumentNullException.ThrowIfNull(options);

        _schema = schema;
        _operationCompiler = operationCompiler;
        _mergeRewriter = new MergeSelectionSetRewriter(schema);
        _partitioner = new SelectionSetPartitioner(schema);
        _selectionSetByTypePartitioner = new SelectionSetByTypePartitioner(schema);
        _nodeFieldSelectionSetPartitioner = new NodeFieldSelectionSetPartitioner(schema);
        _sourceSchemaNodeCandidateResolver = new SourceSchemaNodeCandidateResolver(schema);
        _options = options;
        var policySnapshot = schema.Policies.GetSnapshot();
        _policyState = CreatePolicyPlanningState(policySnapshot);
    }

    internal OperationPlannerOptions Options => _options;

    /// <summary>
    /// Gets the policy planning state for the policy snapshot currently published by the schema.
    /// A plan is built entirely from one such state, read once at the start of <see cref="CreatePlan(string, string, string, OperationDefinitionNode, CancellationToken)"/>,
    /// so that every fetch and condition it produces is consistent with a single published policy set.
    /// </summary>
    private PolicyPlanningState GetPolicyState(ImmutableArray<IPolicy> snapshot)
    {
        var state = Volatile.Read(ref _policyState);

        // ImmutableArray<T> equality is reference equality on the backing array, so this is an
        // O(1) identity check with zero allocations on the common case where the policy set has
        // not changed since the last plan.
        if (state.Source == snapshot)
        {
            return state;
        }

        state = CreatePolicyPlanningState(snapshot);
        Volatile.Write(ref _policyState, state);
        return state;
    }

    /// <summary>
    /// Captures everything the planner derives from one published policy snapshot: the
    /// requirement selection set per policy name and the request-cacheability classification per
    /// policy name. A plan reads one instance of this state throughout its planning pass.
    /// </summary>
    internal sealed record PolicyPlanningState(
        ImmutableArray<IPolicy> Source,
        Dictionary<string, SelectionSetNode> Requirements,
        Dictionary<string, bool> RequestCacheability,
        Dictionary<string, ulong> RequirementHashes);

    private sealed record PolicyPlanningSession(
        PolicyPlanningState Policies,
        ClientIncludeConditionRegistry IncludeConditions,
        ImmutableHashSet<string> RequirementFeedPaths);

    private sealed class ClientIncludeConditionRegistry
    {
        private readonly IncludeConditionCollection _conditions;
        private readonly Dictionary<DirectiveNode, ulong> _masksByDirective;

        private ClientIncludeConditionRegistry(
            IncludeConditionCollection conditions,
            Dictionary<DirectiveNode, ulong> masksByDirective)
        {
            _conditions = conditions;
            _masksByDirective = masksByDirective;
        }

        public IncludeConditionCollection Conditions => _conditions;

        public static ClientIncludeConditionRegistry Create(OperationDefinitionNode operation)
        {
            var conditions = OperationCompiler.CreateIncludeConditionCollection(operation);
            var masksByDirective = new Dictionary<DirectiveNode, ulong>(ReferenceEqualityComparer.Instance);
            ClientIncludeConditionVisitor.Instance.Visit(
                operation,
                new ClientIncludeConditionVisitor.Context(conditions, masksByDirective));
            return new ClientIncludeConditionRegistry(conditions, masksByDirective);
        }

        public ulong CreateGuardMask(ExecutionNodeCondition[] conditions)
        {
            var mask = 0UL;

            foreach (var condition in conditions)
            {
                if (condition.Directive is { } directive
                    && _masksByDirective.TryGetValue(directive, out var directiveMask))
                {
                    mask |= directiveMask;
                    continue;
                }

                for (var i = 0; i < _conditions.Count; i++)
                {
                    var includeCondition = _conditions[i];
                    if ((condition.PassingValue
                            && includeCondition.Include?.Equals(
                                condition.VariableName,
                                StringComparison.Ordinal) == true)
                        || (!condition.PassingValue
                            && includeCondition.Skip?.Equals(
                                condition.VariableName,
                                StringComparison.Ordinal) == true))
                    {
                        mask |= 1UL << i;
                    }
                }
            }

            return mask;
        }

        private sealed class ClientIncludeConditionVisitor
            : SyntaxWalker<ClientIncludeConditionVisitor.Context>
        {
            public static ClientIncludeConditionVisitor Instance { get; } = new();

            protected override HotChocolate.Language.Visitors.ISyntaxVisitorAction Enter(
                FieldNode node,
                Context context)
            {
                context.Register(node, node.Directives);
                return base.Enter(node, context);
            }

            protected override HotChocolate.Language.Visitors.ISyntaxVisitorAction Enter(
                InlineFragmentNode node,
                Context context)
            {
                context.Register(node, node.Directives);
                return base.Enter(node, context);
            }

            internal sealed class Context(
                IncludeConditionCollection conditions,
                Dictionary<DirectiveNode, ulong> masksByDirective)
            {
                public void Register(ISyntaxNode node, IReadOnlyList<DirectiveNode> directives)
                {
                    IncludeCondition condition;
                    var hasCondition = node switch
                    {
                        FieldNode field => IncludeCondition.TryCreate(field, out condition),
                        InlineFragmentNode fragment => IncludeCondition.TryCreate(fragment, out condition),
                        _ => SetDefault(out condition)
                    };

                    if (!hasCondition)
                    {
                        return;
                    }

                    var index = conditions.IndexOf(condition);
                    if (index < 0)
                    {
                        throw HotChocolate.Fusion.Execution.ThrowHelper.InvalidOperationPlan(
                            "A client include condition is missing from the operation-wide condition universe.");
                    }

                    var mask = 1UL << index;
                    foreach (var directive in directives)
                    {
                        if (directive.Name.Value is "skip" or "include")
                        {
                            masksByDirective[directive] = mask;
                        }
                    }
                }

                private static bool SetDefault(out IncludeCondition condition)
                {
                    condition = default;
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// Creates an operation plan for the given operation definition.
    /// </summary>
    /// <param name="id">The unique identifier for operation.</param>
    /// <param name="hash">The hash of the operation document.</param>
    /// <param name="shortHash">The short hash of the operation document.</param>
    /// <param name="operationDefinition">The operation definition to create a plan for.</param>
    /// <param name="cancellationToken">A token that can be used to cancel planning.</param>
    /// <returns>The operation plan.</returns>
    public OperationPlan CreatePlan(
        string id,
        string hash,
        string shortHash,
        OperationDefinitionNode operationDefinition,
        CancellationToken cancellationToken = default)
        => CreatePlan(
            id,
            hash,
            shortHash,
            operationDefinition,
            _schema.Policies.GetSnapshot(),
            cancellationToken);

    internal OperationPlan CreatePlan(
        string id,
        string hash,
        string shortHash,
        OperationDefinitionNode operationDefinition,
        ImmutableArray<IPolicy> policySnapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(hash);
        ArgumentException.ThrowIfNullOrEmpty(shortHash);
        ArgumentNullException.ThrowIfNull(operationDefinition);

        // We make sire that the cancellation token is observed right at the beginning of the method,
        // so that if the caller passed in an already canceled token we don't do any unnecessary work.
        cancellationToken.ThrowIfCancellationRequested();

        var eventSource = PlannerEventSource.Log;
        var eventSourceEnabled = eventSource.IsEnabled();
        var operationType = operationDefinition.Operation.ToString();
        var rootSelectionCount = operationDefinition.SelectionSet.Selections.Count;
        var startedAt = eventSourceEnabled ? Stopwatch.GetTimestamp() : 0L;
        var searchSpace = 0;
        var expandedNodes = 0;
        var stepCount = 0;

        if (eventSourceEnabled)
        {
            eventSource.PlanStart(id, operationType, rootSelectionCount);
        }

        try
        {
            // Read the policy snapshot once for the whole planning pass, so that every fetch
            // and condition this plan produces is built against a single published policy set,
            // even if the schema's policy collection publishes a new set concurrently.
            var policyState = GetPolicyState(policySnapshot);

            if (policyState.Requirements.Count > 0)
            {
                operationDefinition = InjectPolicyRequirements(operationDefinition, policyState);
            }

            // Interface fields whose ownership diverges across concrete types (for example after an
            // @override) are expanded into per-concrete inline fragments up front. This runs before
            // the defer split so deferred selections carry the expansion too, and before selection
            // set indexing so the synthesized fragments receive stable ids like any query-authored
            // fragment (the lookup requirement inlining later resolves ids against this operation).
            // The __typename injection for abstract selections runs after planning and naturally
            // covers the expanded fragments. The schema-level guard skips the walk entirely for the
            // common case where no interface field diverges.
            if (SchemaHasDivergentInterfaceFields())
            {
                operationDefinition = ExpandDivergentInterfaceFields(
                    operationDefinition,
                    _schema.GetOperationType(operationDefinition.Operation));
            }

            var planningSession = new PolicyPlanningSession(
                policyState,
                ClientIncludeConditionRegistry.Create(operationDefinition),
                CreatePolicyRequirementFeedPaths(operationDefinition, policyState));

            // Split deferred selections into a main operation and incremental
            // plan descriptors before planning.
            ImmutableArray<DeliveryGroup> deliveryGroups = [];
            ImmutableArray<IncrementalPlan> incrementalPlans = [];
            DeferSplitResult? deferSplit = null;
            DeferPartitioningResult? partitioning = null;
            var mainOperationDefinition = operationDefinition;

            if (_options.EnableDefer && DeferOperationRewriter.HasDeferDirective(operationDefinition))
            {
                // The partitioner walks the original AST once and hands every
                // @defer fragment a canonical DeliveryGroup instance (with Id,
                // Path and IfVariable populated). The rewriter consumes the
                // same instances so its per-set grouping and the compiler's
                // later Selection._deliveryGroups entries share object identity.
                var deferConditions = new DeferConditionCollection();
                partitioning = DeferPartitioner.Partition(operationDefinition, deferConditions);

                var rewriter = new DeferOperationRewriter(_options.InlineUnlabeledDeferFragments);
                var splitResult = rewriter.Split(operationDefinition, partitioning);

                if (!splitResult.IncrementalPlanDescriptors.IsEmpty)
                {
                    deferSplit = splitResult;
                    mainOperationDefinition = splitResult.MainOperation;
                }
            }

            // We first need to create an index to keep track of the logical selections
            // sets before we can branch them. This allows us to inline requirements later
            // into the right place.
            var index = SelectionSetIndexer.Create(mainOperationDefinition);

            // Next, we create the seed plan with a set of initial work items exploring the root selection set.
            var (node, selectionSet) = mainOperationDefinition.Operation switch
            {
                OperationType.Query => CreateQueryPlanBase(mainOperationDefinition, shortHash, index),
                OperationType.Mutation => CreateMutationPlanBase(mainOperationDefinition, shortHash, index),
                OperationType.Subscription => CreateSubscriptionPlanBase(mainOperationDefinition, shortHash, index),
                _ => throw new ArgumentOutOfRangeException()
            };

            var internalOperationDefinition = mainOperationDefinition;
            ImmutableList<PlanStep> planSteps = [];
            var policySlots = PolicySlotRegistry.Empty;

            // The backlog is only empty for pure introspection queries, which the
            // gateway serves directly without planning against any source schema.
            if (!node.Backlog.IsEmpty)
            {
                var possiblePlans = new PlanQueue(_schema);

                if (mainOperationDefinition.Operation == OperationType.Subscription
                    && TryResolveEventStream(selectionSet, out var subscriptionField))
                {
                    var eventStreamPlan = PlanEventStreamSubscription(
                        id,
                        node,
                        subscriptionField,
                        planningSession,
                        eventSourceEnabled,
                        cancellationToken);

                    internalOperationDefinition = eventStreamPlan.InternalOperationDefinition;
                    planSteps = eventStreamPlan.Steps;
                    searchSpace = eventStreamPlan.SearchSpace;
                    expandedNodes = eventStreamPlan.ExpandedNodes;
                    stepCount = eventStreamPlan.StepCount;
                    policySlots = eventStreamPlan.PolicySlots;
                }
                else
                {
                    // Enqueue a seed plan per candidate schema, each represents a possible plan branch.
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

                if (planSteps.IsEmpty)
                {
                    // For plans that cannot be branched we simply enqueue the plan node.
                    // This often happens when we have computed fields like `node` which will be
                    // expanded later.
                    if (possiblePlans.Count < 1)
                    {
                        possiblePlans.Enqueue(node);
                    }

                    // Now that we have seeded the possible plans we can start planning.
                    var plan = Plan(id, possiblePlans, planningSession, eventSourceEnabled, cancellationToken);

                    if (!plan.HasValue)
                    {
                        throw new InvalidOperationException("No possible plan was found.");
                    }

                    internalOperationDefinition = plan.Value.InternalOperationDefinition;
                    planSteps = plan.Value.Steps;
                    searchSpace = plan.Value.SearchSpace;
                    expandedNodes = plan.Value.ExpandedNodes;
                    stepCount = plan.Value.StepCount;
                    policySlots = plan.Value.PolicySlots;
                }

                internalOperationDefinition =
                    AddTypeNameToAbstractSelections(
                        internalOperationDefinition,
                        _schema.GetOperationType(mainOperationDefinition.Operation));
            }

            // Prepare incremental plans before the root operation is compiled
            // so parent-scope requirements are represented in the root plan.
            PlanContextGraph? deferContextGraph = null;
            ImmutableArray<DeferRoutingState> deferRoutingStates = [];

            if (deferSplit.HasValue && partitioning is not null)
            {
                deliveryGroups = partitioning.AllDeliveryGroups;

                deferContextGraph = PlanContextGraph.Create(
                    planSteps,
                    index,
                    internalOperationDefinition);

                deferRoutingStates = RouteIncrementalPlans(
                    id,
                    deferSplit.Value,
                    deferContextGraph,
                    planningSession,
                    ref policySlots,
                    eventSourceEnabled,
                    cancellationToken);

                // Any parent-scope transformations applied while routing
                // defer requirements flow back into the root step list and
                // the root internal operation definition that compile and
                // the execution tree builder consume below.
                planSteps = deferContextGraph.RootSteps;
                internalOperationDefinition = deferContextGraph.RootInternalOperation;

                // A descriptor whose deferred output turned out to be fully redundant with
                // data the enclosing scope already fetches is routed without producing an
                // incremental plan (see TryAbsorbFullyRedundantDefer); its own delivery
                // group(s) must not surface as a pending entry with nothing to complete it.
                if (deferRoutingStates.Length < deferSplit.Value.IncrementalPlanDescriptors.Length)
                {
                    var usedDeliveryGroupIds = new HashSet<int>();
                    foreach (var routingState in deferRoutingStates)
                    {
                        foreach (var group in routingState.Descriptor.DeliveryGroupSet)
                        {
                            usedDeliveryGroupIds.Add(group.Id);
                        }
                    }

                    var filteredDeliveryGroups = ImmutableArray.CreateBuilder<DeliveryGroup>(
                        usedDeliveryGroupIds.Count);
                    foreach (var group in deliveryGroups)
                    {
                        if (usedDeliveryGroupIds.Contains(group.Id))
                        {
                            filteredDeliveryGroups.Add(group);
                        }
                    }

                    deliveryGroups = filteredDeliveryGroups.ToImmutable();
                }
            }

            // Use the latest root operation definition after deferred planning.
            var operation = _operationCompiler.Compile(
                id,
                hash,
                shortHash,
                internalOperationDefinition,
                planningSession.IncludeConditions.Conditions);

            if (deferContextGraph is not null)
            {
                incrementalPlans = BuildIncrementalPlans(
                    id,
                    hash,
                    shortHash,
                    deferRoutingStates,
                    deferContextGraph,
                    planningSession.IncludeConditions.Conditions,
                    policySlots.Entries,
                    cancellationToken);
            }

            var operationPlan = BuildExecutionPlan(
                operation,
                mainOperationDefinition,
                planSteps,
                deliveryGroups,
                incrementalPlans,
                policySlots.Expressions,
                policySlots.Entries,
                policySlots.Policies,
                new PolicyArtifactPolicySnapshot(
                    planningSession.Policies.RequestCacheability,
                    planningSession.Policies.Requirements,
                    planningSession.Policies.RequirementHashes),
                searchSpace,
                expandedNodes,
                planSteps.NextId(),
                cancellationToken);

            if (eventSourceEnabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                eventSource.PlanStop(
                    id,
                    (long)elapsed.TotalMilliseconds,
                    searchSpace,
                    expandedNodes,
                    stepCount);
            }

            return operationPlan;
        }
        catch (Exception ex)
        {
            if (eventSourceEnabled)
            {
                var elapsed = Stopwatch.GetElapsedTime(startedAt);
                eventSource.PlanError(
                    id,
                    operationType,
                    ex.GetType().FullName ?? ex.GetType().Name,
                    (long)elapsed.TotalMilliseconds);
            }

            throw;
        }
    }

    /// <summary>
    /// Determines whether the step-dependency graph of the given plan steps contains a cycle.
    /// The graph is derived by inverting each step's dependents, then traversed depth-first.
    /// </summary>
    private static bool HasCyclicStepDependencies(ImmutableList<PlanStep> steps)
    {
        Dictionary<int, List<int>>? dependencies = null;

        foreach (var step in steps)
        {
            if (step is OperationPlanStep operationStep)
            {
                foreach (var dependentId in operationStep.Dependents)
                {
                    dependencies ??= [];

                    if (!dependencies.TryGetValue(dependentId, out var list))
                    {
                        list = [];
                        dependencies[dependentId] = list;
                    }

                    list.Add(operationStep.Id);
                }
            }
        }

        if (dependencies is null)
        {
            return false;
        }

        var visited = new HashSet<int>();
        var stack = new HashSet<int>();

        foreach (var stepId in dependencies.Keys)
        {
            if (HasCycle(stepId, dependencies, visited, stack))
            {
                return true;
            }
        }

        return false;

        static bool HasCycle(
            int stepId,
            Dictionary<int, List<int>> dependencies,
            HashSet<int> visited,
            HashSet<int> stack)
        {
            if (stack.Contains(stepId))
            {
                return true;
            }

            if (!visited.Add(stepId))
            {
                return false;
            }

            stack.Add(stepId);

            if (dependencies.TryGetValue(stepId, out var deps))
            {
                foreach (var dep in deps)
                {
                    if (HasCycle(dep, dependencies, visited, stack))
                    {
                        return true;
                    }
                }
            }

            stack.Remove(stepId);
            return false;
        }
    }

    private PlanResult? Plan(
        string operationId,
        PlanQueue possiblePlans,
        PolicyPlanningSession planningSession,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        var eventSource = PlannerEventSource.Log;
        var searchSpace = possiblePlans.Count;
        var expandedNodes = 0;
        var maxPlanningTime = _options.MaxPlanningTime;
        var maxExpandedNodes = _options.MaxExpandedNodes;
        var maxQueueSize = _options.MaxQueueSize;
        var maxGeneratedOptionsPerWorkItem = _options.MaxGeneratedOptionsPerWorkItem;
        var planningStartedAt = maxPlanningTime.HasValue ? Stopwatch.GetTimestamp() : 0L;

        // TryBuildGreedyCompletePlan quickly builds one full plan by always choosing the currently
        // cheapest next option at each step.
        //
        // It gives the planner an initial best known complete cost, so the main search can skip branches
        // that are already worse. If it cannot finish a full plan, it returns null and the planner
        // continues without that early shortcut.
        var bestCompletePlan = TryBuildGreedyCompletePlan(possiblePlans, planningSession, cancellationToken);

        // A plan whose step-dependency graph is cyclic cannot be scheduled, so it must never win.
        // We discard a cyclic greedy plan here and reject cyclic candidates during the search below.
        if (bestCompletePlan is not null && HasCyclicStepDependencies(bestCompletePlan.Steps))
        {
            bestCompletePlan = null;
        }

        var bestCompletePlanCost = bestCompletePlan?.PathCost ?? double.PositiveInfinity;

        while (possiblePlans.TryDequeue(out var current, out _))
        {
            // we evaluate the cancellationToken at the beginning of each plan evaluation loop,
            // so that we throw ones a request was canceled so that no unnecessary work is done.
            cancellationToken.ThrowIfCancellationRequested();

            expandedNodes++;
            var possiblePlansCount = possiblePlans.Count;
            searchSpace = Math.Max(possiblePlansCount, searchSpace);

            // before we get into another planning iteration, we check if we have
            // exceeded any of the configured guardrails and throw if so.
            EnsurePlanningTimeGuardrail();
            EnsureExpandedNodesGuardrail(expandedNodes);
            EnsureQueueSizeGuardrail(possiblePlansCount);

            var backlog = current.Backlog;

            if (emitPlannerEvents)
            {
                eventSource.PlanDequeue(
                    operationId,
                    expandedNodes,
                    possiblePlansCount,
                    backlog.IsEmpty ? "Complete" : FormatWorkItemName(backlog.Peek()),
                    current.SchemaName);
            }

            // If the current plan is already at least as expensive as the
            // best complete plan, we can skip it and don't need to evaluate
            // it any further.
            if (current.BestCaseCost >= bestCompletePlanCost)
            {
                continue;
            }

            if (backlog.IsEmpty)
            {
                // Reject candidates whose step-dependency graph is cyclic; they cannot be scheduled.
                if (HasCyclicStepDependencies(current.Steps))
                {
                    continue;
                }

                // We found a complete plan. Keep it if it is cheaper than the current best plan.
                // If cost is the same, use a deterministic tie-break so results stay stable.
                var completeCost = current.PathCost;

                if (completeCost < bestCompletePlanCost
                    || (completeCost.Equals(bestCompletePlanCost)
                        && bestCompletePlan is not null
                        && ComparePlansForTieBreak(current, bestCompletePlan) < 0))
                {
                    bestCompletePlan = current;
                    bestCompletePlanCost = completeCost;
                }

                continue;
            }

            // The backlog represents the tasks we have to complete to build out
            // the current possible plan. It's not guaranteed that this plan will work
            // out or that it is efficient.
            backlog = current.Backlog.Pop(out var workItem);
            var queueCountBeforeExpansion = possiblePlans.Count;

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanRootSelections(wi, current, backlog, possiblePlans, planningSession);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, possiblePlans, planningSession);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        possiblePlans,
                        backlog,
                        planningSession);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        possiblePlans,
                        backlog,
                        planningSession);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, possiblePlans, backlog, planningSession);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, possiblePlans, backlog, planningSession);
                    break;

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }

            // after we have expanded the current plan node into possible next steps,
            // we check how many new plans we have created and if we have exceeded
            // the guardrail for generated options per work item.
            var queueCountAfterExpansion = possiblePlans.Count;
            searchSpace = Math.Max(queueCountAfterExpansion, searchSpace);
            EnsureGeneratedOptionsGuardrail(queueCountBeforeExpansion, queueCountAfterExpansion);
        }

        if (bestCompletePlan is null)
        {
            return null;
        }

        if (emitPlannerEvents)
        {
            for (var i = 0; i < bestCompletePlan.PolicySlots.OverflowedIdentityCount; i++)
            {
                eventSource.PolicySlotCapacityExceeded(operationId, PolicySlotRegistry.MaxPolicySlots);
            }
        }

        return new PlanResult(
            bestCompletePlan.InternalOperationDefinition,
            bestCompletePlan.Steps,
            searchSpace,
            expandedNodes,
            bestCompletePlan.OperationStepCount,
            bestCompletePlan.PolicySlots);

        static string FormatWorkItemName(WorkItem workItem)
            => workItem switch
            {
                OperationWorkItem { Kind: OperationWorkItemKind.Root } => "OperationRoot",
                OperationWorkItem { Kind: OperationWorkItemKind.Lookup } => "OperationLookup",
                FieldRequirementWorkItem { Lookup: null } => "FieldRequirementInline",
                FieldRequirementWorkItem => "FieldRequirementLookup",
                NodeFieldWorkItem => "NodeField",
                NodeLookupWorkItem { Lookup: null } => "NodeLookup",
                NodeLookupWorkItem => "NodeLookupBound",
                _ => "Unknown"
            };

        void EnsurePlanningTimeGuardrail()
        {
            if (maxPlanningTime is not { } planningTimeLimit)
            {
                return;
            }

            var elapsed = Stopwatch.GetElapsedTime(planningStartedAt);
            if (elapsed < planningTimeLimit)
            {
                return;
            }

            ThrowGuardrailExceeded(
                OperationPlannerGuardrailReason.MaxPlanningTimeExceeded,
                ToGuardrailMilliseconds(planningTimeLimit),
                ToGuardrailMilliseconds(elapsed));
        }

        void EnsureExpandedNodesGuardrail(int currentExpandedNodes)
        {
            if (maxExpandedNodes is not { } expandedNodesLimit
                || currentExpandedNodes <= expandedNodesLimit)
            {
                return;
            }

            ThrowGuardrailExceeded(
                OperationPlannerGuardrailReason.MaxExpandedNodesExceeded,
                expandedNodesLimit,
                currentExpandedNodes);
        }

        void EnsureQueueSizeGuardrail(int queueSize)
        {
            if (maxQueueSize is not { } queueSizeLimit
                || queueSize <= queueSizeLimit)
            {
                return;
            }

            ThrowGuardrailExceeded(
                OperationPlannerGuardrailReason.MaxQueueSizeExceeded,
                queueSizeLimit,
                queueSize);
        }

        void EnsureGeneratedOptionsGuardrail(int queueCountBeforeExpansion, int queueCountAfterExpansion)
        {
            if (maxGeneratedOptionsPerWorkItem is not { } generatedOptionsLimit)
            {
                return;
            }

            var generatedOptions = queueCountAfterExpansion - queueCountBeforeExpansion;
            if (generatedOptions <= generatedOptionsLimit)
            {
                return;
            }

            ThrowGuardrailExceeded(
                OperationPlannerGuardrailReason.MaxGeneratedOptionsPerWorkItemExceeded,
                generatedOptionsLimit,
                generatedOptions);
        }

        void ThrowGuardrailExceeded(
            OperationPlannerGuardrailReason reason,
            long limit,
            long observed)
        {
            if (emitPlannerEvents)
            {
                eventSource.PlanGuardrailExceeded(
                    operationId,
                    reason.ToString(),
                    limit,
                    observed);
            }

            throw new OperationPlannerGuardrailException(
                operationId,
                reason,
                limit,
                observed);
        }

        static long ToGuardrailMilliseconds(TimeSpan value)
            => checked((long)Math.Ceiling(value.TotalMilliseconds));
    }

    private PlanNode? TryBuildGreedyCompletePlan(
        PlanQueue possiblePlans,
        PolicyPlanningSession planningSession,
        CancellationToken cancellationToken)
    {
        if (!possiblePlans.TryPeek(out var current, out _))
        {
            return null;
        }

        var candidates = new PlanQueue(_schema);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var backlog = current.Backlog;

            if (backlog.IsEmpty)
            {
                return current;
            }

            backlog = backlog.Pop(out var workItem);

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanRootSelections(wi, current, backlog, candidates, planningSession);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, candidates, planningSession);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        candidates,
                        backlog,
                        planningSession);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        candidates,
                        backlog,
                        planningSession);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, candidates, backlog, planningSession);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, candidates, backlog, planningSession);
                    break;

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }

            if (!candidates.TryDequeue(out current, out _))
            {
                return null;
            }

            candidates.Clear();
        }
    }

    private static OperationStepCostState AddOperationStepCostState(PlanNode current, int stepId, int stepDepth)
    {
        // Planner transitions create many nodes. We keep all path-cost counters updated
        // incrementally here to avoid walking existing steps for depth/fan-out recomputation.
        var previousOpsAtDepth = current.OpsPerLevel.GetValueOrDefault(stepDepth, 0);
        var nextOpsAtDepth = previousOpsAtDepth + 1;
        var opsPerLevel = current.OpsPerLevel.SetItem(stepDepth, nextOpsAtDepth);
        var excessFanout = current.ExcessFanout;

        if (previousOpsAtDepth >= current.Options.FanoutPenaltyThreshold)
        {
            excessFanout++;
        }

        return new OperationStepCostState(
            Math.Max(current.MaxDepth, stepDepth),
            excessFanout,
            opsPerLevel,
            current.OperationStepDepths.SetItem(stepId, stepDepth));
    }

    private static int GetOperationStepDepth(PlanNode current, int stepId)
        => current.OperationStepDepths.TryGetValue(stepId, out var stepDepth)
            ? stepDepth
            : 1;

    private static int ComparePlansForTieBreak(PlanNode left, PlanNode right)
    {
        var stepCountComparison = left.OperationStepCount.CompareTo(right.OperationStepCount);
        if (stepCountComparison != 0)
        {
            return stepCountComparison;
        }

        var stepsComparison = left.Steps.Count.CompareTo(right.Steps.Count);
        if (stepsComparison != 0)
        {
            return stepsComparison;
        }

        for (var i = 0; i < left.Steps.Count; i++)
        {
            var comparison = left.Steps[i].Id.CompareTo(right.Steps[i].Id);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareStepDeterministically(left.Steps[i], right.Steps[i]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return string.CompareOrdinal(left.SchemaName, right.SchemaName);

        static int CompareStepDeterministically(PlanStep leftStep, PlanStep rightStep)
        {
            if (leftStep is OperationPlanStep leftOperationStep
                && rightStep is OperationPlanStep rightOperationStep)
            {
                var comparison = string.CompareOrdinal(
                    leftOperationStep.SchemaName,
                    rightOperationStep.SchemaName);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = leftOperationStep.RootSelectionSetId.CompareTo(rightOperationStep.RootSelectionSetId);
                if (comparison != 0)
                {
                    return comparison;
                }

                comparison = leftOperationStep.Definition.SelectionSet.Selections.Count.CompareTo(
                    rightOperationStep.Definition.SelectionSet.Selections.Count);
                if (comparison != 0)
                {
                    return comparison;
                }

                return string.CompareOrdinal(
                    leftOperationStep.Definition.Name?.Value,
                    rightOperationStep.Definition.Name?.Value);
            }

            if (leftStep is NodeFieldPlanStep leftNodeStep
                && rightStep is NodeFieldPlanStep rightNodeStep)
            {
                return string.CompareOrdinal(leftNodeStep.ResponseName, rightNodeStep.ResponseName);
            }

            // Prefer operation steps over node fallback steps when everything else is equal.
            return leftStep switch
            {
                OperationPlanStep when rightStep is NodeFieldPlanStep => -1,
                NodeFieldPlanStep when rightStep is OperationPlanStep => 1,
                _ => string.CompareOrdinal(leftStep.GetType().Name, rightStep.GetType().Name)
            };
        }
    }

    private static bool TryResolveEventStream(
        SelectionSet selectionSet,
        out SubscriptionField subscriptionField)
    {
        subscriptionField = default;

        if (selectionSet.Type is not FusionComplexTypeDefinition complexType)
        {
            return false;
        }

        FieldNode? rootFieldNode = null;

        foreach (var selection in selectionSet.Selections)
        {
            if (selection is FieldNode fieldNode
                && !fieldNode.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal))
            {
                rootFieldNode = fieldNode;
                break;
            }
        }

        if (rootFieldNode is null)
        {
            return false;
        }

        var field = complexType.Fields.GetField(rootFieldNode.Name.Value, allowInaccessibleFields: true);
        var responseName = rootFieldNode.Alias?.Value ?? rootFieldNode.Name.Value;

        foreach (var source in field.Sources.Members)
        {
            if (source.EventStreamDirective is { } directive)
            {
                subscriptionField = new SubscriptionField(responseName, source.SchemaName, directive);
                return true;
            }
        }

        return false;
    }

    private EventStreamPlanningResult PlanEventStreamSubscription(
        string operationId,
        PlanNode seed,
        SubscriptionField subscriptionField,
        PolicyPlanningSession planningSession,
        bool emitPlannerEvents,
        CancellationToken cancellationToken)
    {
        var possiblePlans = new PlanQueue(_schema);
        possiblePlans.Enqueue(
            seed with
            {
                SchemaName = subscriptionField.SchemaName,
                EventStreamDirective = subscriptionField.Directive,
                ResolutionCost = 0
            });

        var plan = Plan(operationId, possiblePlans, planningSession, emitPlannerEvents, cancellationToken);

        if (!plan.HasValue)
        {
            throw new InvalidOperationException("No possible event stream plan was found.");
        }

        var rootStep = GetEventStreamRootStep(plan.Value.Steps);
        var eventStreamPlan = new EventStreamPlan
        {
            FieldName = subscriptionField.Name,
            Source = CreateEventStreamSource(subscriptionField),
            Message = subscriptionField.Directive.Message.ToString(indented: false)
        };

        var rootStepIndex = plan.Value.Steps.IndexOf(rootStep);
        var eventStreamMessage = CreateEventStreamMessageSelectionSet(subscriptionField.Directive);
        var updatedRootStep = rootStep with
        {
            Definition = CreateEventStreamMessageOperationDefinition(
                rootStep.Definition,
                [eventStreamMessage]),
            EventStreamPlan = eventStreamPlan
        };
        var steps = plan.Value.Steps.SetItem(rootStepIndex, updatedRootStep);

        return new EventStreamPlanningResult(
            plan.Value.InternalOperationDefinition,
            steps,
            plan.Value.SearchSpace,
            plan.Value.ExpandedNodes,
            plan.Value.StepCount,
            plan.Value.PolicySlots);
    }

    private OperationDefinitionNode CreateEventStreamMessageOperationDefinition(
        OperationDefinitionNode definition,
        IReadOnlyList<SelectionSetNode> messageSelectionSets)
    {
        if (messageSelectionSets.Count == 0)
        {
            return definition;
        }

        var rootField = GetSingleRootField(definition);
        var fieldType = GetSubscriptionFieldType(rootField);
        var mergedMessageSelectionSet = messageSelectionSets.Count == 1
            ? messageSelectionSets[0]
            : _mergeRewriter.Merge(messageSelectionSets, fieldType);

        return definition.WithSelectionSet(
            new SelectionSetNode([rootField.WithSelectionSet(mergedMessageSelectionSet)]));
    }

    private FieldNode GetSingleRootField(OperationDefinitionNode definition)
    {
        foreach (var selection in definition.SelectionSet.Selections)
        {
            if (selection is FieldNode field
                && !field.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new InvalidOperationException("The event stream operation must contain a root field.");
    }

    private ITypeDefinition GetSubscriptionFieldType(FieldNode rootField)
    {
        if (_schema.GetOperationType(OperationType.Subscription) is not FusionComplexTypeDefinition subscriptionType)
        {
            throw new InvalidOperationException("The subscription operation type must be a complex type.");
        }

        var field = subscriptionType.Fields.GetField(rootField.Name.Value, allowInaccessibleFields: true);
        return field.Type.AsTypeDefinition();
    }

    private static EventStreamSource CreateEventStreamSource(SubscriptionField subscriptionField)
        => new()
        {
            SchemaName = subscriptionField.SchemaName,
            FieldName = subscriptionField.Name,
            Topics = subscriptionField.Directive.Topics,
            Broker = subscriptionField.Directive.Broker,
            Message = subscriptionField.Directive.Message,
            CursorField = subscriptionField.Directive.CursorField,
            CursorArgument = subscriptionField.Directive.CursorArgument
        };

    private static SelectionSetNode CreateEventStreamMessageSelectionSet(
        EventStreamDirective directive)
    {
        if (string.IsNullOrEmpty(directive.CursorField))
        {
            return directive.Message;
        }

        return directive.Message.WithSelections(
            [
                .. directive.Message.Selections,
                new FieldNode(directive.CursorField)
            ]);
    }

    private static OperationPlanStep GetEventStreamRootStep(ImmutableList<PlanStep> steps)
        => steps
            .OfType<OperationPlanStep>()
            .Single(t => t.Target.IsRoot && t.Definition.Operation == OperationType.Subscription);

    private static SelectionSetNode? CreateEventStreamProvidedSelectionSet(
        SelectionSetNode rootSelectionSet,
        EventStreamDirective directive)
    {
        foreach (var selection in rootSelectionSet.Selections)
        {
            if (selection is FieldNode fieldNode
                && !fieldNode.Name.Value.Equals(IntrospectionFieldNames.TypeName, StringComparison.Ordinal))
            {
                return new SelectionSetNode([
                    fieldNode.WithSelectionSet(CreateEventStreamMessageSelectionSet(directive))
                ]);
            }
        }

        return null;
    }

    private readonly record struct SubscriptionField(
        string Name,
        string SchemaName,
        EventStreamDirective Directive);

    private readonly record struct EventStreamPlanningResult(
        OperationDefinitionNode InternalOperationDefinition,
        ImmutableList<PlanStep> Steps,
        int SearchSpace,
        int ExpandedNodes,
        int StepCount,
        PolicySlotRegistry PolicySlots);

    private void PlanRootSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Backlog backlog,
        PlanQueue possiblePlans,
        PolicyPlanningSession planningSession)
        => PlanSelections(workItem, current, null, backlog, possiblePlans, planningSession);

    private void PlanLookupSelections(
        OperationWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        Backlog backlog,
        PlanQueue possiblePlans,
        PolicyPlanningSession planningSession)
    {
        current = InlineLookupRequirements(
            workItem.SelectionSet,
            current,
            lookup,
            workItem.EstimatedDepth,
            backlog,
            workItem.Conditions,
            workItem.AllowSourceSchemaReentry,
            workItem.SourceSchemaNodePolicy,
            out var unresolvedRequirements);

        // A self-cyclic lookup can proceed only when an existing step supplies its key.
        // If requirement inlining leaves the identical selection unresolved, this direct
        // branch made no progress. The parent-path alternatives were enqueued separately.
        if (unresolvedRequirements is not null
            && PlanQueue.IsSelfCyclicLookup(workItem, lookup)
            && SyntaxComparer.BySyntax.Equals(
                unresolvedRequirements,
                workItem.SelectionSet.Node))
        {
            return;
        }

        PlanSelections(
            workItem,
            current,
            lookup,
            current.Backlog,
            possiblePlans,
            planningSession);
    }

    private void PlanSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Lookup? lookup,
        Backlog backlog,
        PlanQueue possiblePlans,
        PolicyPlanningSession planningSession)
    {
        var stepId = current.Steps.NextId();
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;

        // The event stream message describes the shape the broker delivers. Fields the client
        // selected beyond that shape are spilled even though the source schema owns them, so the
        // lookups that resolve them are allowed to re-enter the source schema itself.
        var isEventStreamRoot = workItem.Kind is OperationWorkItemKind.Root
            && current.EventStreamDirective is not null;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = workItem.SelectionSet,
            SelectionSetIndex = index,
            Conditions = workItem.Conditions,
            PruneUnprovidedAbstractBranches = isEventStreamRoot,
            ProvidedSelectionSet = isEventStreamRoot
                ? CreateEventStreamProvidedSelectionSet(
                    workItem.SelectionSet.Node,
                    current.EventStreamDirective!)
                : null,
            TreatSourceExternalAsUnresolvable = workItem.SourceSchemaNodePolicy is not null
        };

        (var resolvable, var unresolvable, var fieldsWithRequirements, var policyTargets, index) =
            _partitioner.Partition(input);

        if (resolvable is not { Selections.Count: > 0 }
            && workItem.SourceSchemaNodePolicy is not null
            && !fieldsWithRequirements.IsEmpty)
        {
            resolvable = new SelectionSetNode([new FieldNode(IntrospectionFieldNames.TypeName)]);
            var strictIndexBuilder = index.ToBuilder();
            strictIndexBuilder.Register(workItem.SelectionSet.Id, resolvable);
            index = strictIndexBuilder;
        }

        // if we cannot resolve any selection with the current source schema then this path
        // cannot be used to resolve the data for the current operation, and we need to skip it.
        if (resolvable is null
            || (workItem.SourceSchemaNodePolicy is not null
                && resolvable.Selections.Count == 0))
        {
            return;
        }

        var (targets, policyGates, policySlots) = BuildPolicyTargets(
            policyTargets,
            workItem.SelectionSet,
            workItem.Conditions,
            includeSelectionSetObjectPolicy: workItem.SelectionSet.Path.IsRoot,
            selectionSetGateEligible: !index.IsConcreteBranch(workItem.SelectionSet.Id),
            current.PolicySlots,
            planningSession);
        ExecutionNodeCondition[] slotConditions;

        (resolvable, unresolvable, fieldsWithRequirements, index, slotConditions) =
            ApplyPolicyGates(
                workItem.SelectionSet,
                resolvable,
                unresolvable,
                fieldsWithRequirements,
                index,
                policyGates);

        backlog = backlog.PushUnresolvable(
            unresolvable,
            current.SchemaName,
            stepDepth,
            allowSourceSchemaReentry: isEventStreamRoot,
            sourceSchemaNodePolicy: workItem.SourceSchemaNodePolicy is null
                ? null
                : SourceSchemaNodePlanningPolicy.Descendant);
        backlog = backlog.PushRequirements(
            fieldsWithRequirements,
            new StepConsumer(stepId),
            stepDepth,
            workItem.SourceSchemaNodePolicy is null
                ? null
                : SourceSchemaNodePlanningPolicy.Descendant);

        // Lookups are always queries. Root work items can also be rewritten to the query root
        // when walking shared paths (for example the viewer convention in mutations).
        var operationType =
            lookup is not null || IsQueryRootSelection(workItem.SelectionSet)
                ? OperationType.Query
                : current.OperationDefinition.Operation;

        var operationBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(operationType)
                .SetName(current.CreateOperationName(stepId))
                .SetSelectionSet(resolvable);

        var lastRequirementId = current.LastRequirementId;
        var requirements = ImmutableDictionary<string, OperationRequirement>.Empty;

        if (lookup is not null)
        {
            lastRequirementId++;
            var requirementKey = $"__fusion_{lastRequirementId}";

            for (var i = 0; i < lookup.Arguments.Length; i++)
            {
                var argument = lookup.Arguments[i];
                var fieldSelectionMap = lookup.Fields[i];

                var argumentRequirementKey = $"{requirementKey}_{argument.Name}";
                var operationRequirement = new OperationRequirement(
                    argumentRequirementKey,
                    argument.Type,
                    workItem.SelectionSet.Path,
                    fieldSelectionMap,
                    null);

                requirements = requirements.Add(argumentRequirementKey, operationRequirement);
            }

            operationBuilder.SetLookup(lookup, GetLookupArguments(lookup, requirementKey), workItem.SelectionSet.Type);
        }

        (var definition, index, var source) = operationBuilder.Build(index);

        if (lookup is null
            && operationType == OperationType.Query
            && !workItem.SelectionSet.Path.IsRoot
            && resolvable.Selections is [FieldNode field]
            && PlannerExtensions.IsViewerFieldSelection(field))
        {
            source = SelectionPath.Root.AppendField(field.Name.Value);
        }

        var policyStepId = targets.IsDefaultOrEmpty ? 0 : stepId + 1;
        var stepConditions = slotConditions.Length == 0
            ? workItem.Conditions
            : [.. workItem.Conditions, .. slotConditions];

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(resolvable),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Dependents = policyStepId == 0
                ? workItem.Dependents
                : workItem.Dependents.Add(policyStepId),
            Requirements = requirements,
            Conditions = stepConditions,
            Target = workItem.SelectionSet.Path,
            Source = source,
            Lookup = lookup
        };

        var steps = current.Steps.Add(step);

        if (policyStepId != 0)
        {
            steps = steps.Add(new PolicyPlanStep
            {
                Id = policyStepId,
                Targets = targets
            });
        }

        var costState = AddOperationStepCostState(current, stepId, stepDepth);
        var remainingCost = PlannerCostEstimator.EstimateRemainingCost(
            current.Options,
            costState.MaxDepth,
            costState.OpsPerLevel,
            backlog.Cost);

        var next = new PlanNode
        {
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            RemainingCost = remainingCost,
            Steps = steps,
            LastRequirementId = lastRequirementId,
            RequirementAliases = current.RequirementAliases,
            PolicySlots = policySlots,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths,
            EventStreamDirective = current.EventStreamDirective
        };

        possiblePlans.EnqueueBranches(next);
    }

    /// <summary>
    /// Classifies every policy coordinate reached by an operation step into condition gates and execution-node residuals.
    /// </summary>
    private (
        ImmutableArray<PolicyExecutionTarget> Targets,
        ImmutableArray<PolicyGate> Gates,
        PolicySlotRegistry Slots) BuildPolicyTargets(
        ImmutableStack<ConditionalPolicyExecutionTarget> policyTargets,
        SelectionSet selectionSet,
        ExecutionNodeCondition[] conditions,
        bool includeSelectionSetObjectPolicy,
        bool selectionSetGateEligible,
        PolicySlotRegistry slots,
        PolicyPlanningSession planningSession)
    {
        var coordinates = new List<ConditionalPolicyExecutionTarget>();

        if (includeSelectionSetObjectPolicy)
        {
            if (selectionSet.Type is FusionObjectTypeDefinition objectType)
            {
                var gateEligible = selectionSetGateEligible;
                foreach (var existingTarget in policyTargets)
                {
                    if (existingTarget.Target.Kind is PolicyTargetKind.Object
                        && existingTarget.Target.TypeName.Equals(
                            objectType.Name,
                            StringComparison.Ordinal)
                        && existingTarget.Occurrences.Any(occurrence => !occurrence.GateEligible))
                    {
                        gateEligible = false;
                        break;
                    }
                }

                for (var i = 0; gateEligible && i < selectionSet.Path.Length; i++)
                {
                    if (selectionSet.Path[i].Kind is SelectionPathSegmentKind.InlineFragment)
                    {
                        gateEligible = false;
                        break;
                    }
                }

                AddSelectionSetObjectPolicy(objectType, gateEligible);
            }
            else if (selectionSet.Type is FusionInterfaceTypeDefinition or FusionUnionTypeDefinition)
            {
                foreach (var possibleType in _schema.GetPossibleTypes(
                    selectionSet.Type,
                    includeInaccessible: true))
                {
                    AddSelectionSetObjectPolicy(possibleType, gateEligible: false);
                }
            }
        }

        if (!policyTargets.IsEmpty)
        {
            foreach (var policyTarget in policyTargets.Reverse())
            {
                coordinates.Add(policyTarget with
                {
                    Target = AddPolicyRequirements(policyTarget.Target, planningSession.Policies)
                });
            }
        }

        if (coordinates.Count == 0)
        {
            return ([], [], slots);
        }

        var residualTargets = ImmutableArray.CreateBuilder<PolicyExecutionTarget>();
        var gates = ImmutableArray.CreateBuilder<PolicyGate>();

        for (var coordinateIndex = 0; coordinateIndex < coordinates.Count; coordinateIndex++)
        {
            var policyTarget = coordinates[coordinateIndex];
            var coordinate = policyTarget.Target;
            slots = slots.AddInventory(coordinate.Policies, planningSession.Policies);
            var slotApplications = ImmutableArray.CreateBuilder<PolicyGateApplication>();
            var slotApplicationClasses = ImmutableArray.CreateBuilder<PolicySlotApplicationClass>();
            var residualApplications = new List<PolicyApplication>();
            var hasResidual = false;
            var rmax = PolicyDenialBehavior.Null;

            foreach (var application in coordinate.Policies)
            {
                switch (ClassifyApplication(application, planningSession.Policies))
                {
                    case PolicyApplicationClass.S:
                        slotApplications.Add(CreatePolicyGateApplication(
                            application,
                            planningSession.Policies));
                        slotApplicationClasses.Add(PolicySlotApplicationClass.SlotOnly);
                        break;

                    case PolicyApplicationClass.M:
                        slotApplications.Add(CreatePolicyGateApplication(
                            application,
                            planningSession.Policies));
                        slotApplicationClasses.Add(PolicySlotApplicationClass.SlotAndResidual);
                        residualApplications.Add(application);
                        hasResidual = true;
                        rmax = Max(rmax, application.OnDenied);
                        break;

                    case PolicyApplicationClass.ResidualOnly:
                        residualApplications.Add(application);
                        hasResidual = true;
                        rmax = Max(rmax, application.OnDenied);
                        break;
                }
            }

            if (slotApplications.Count == 0)
            {
                AddResidualTargets(coordinate, policyTarget.Occurrences, condition: null);
                continue;
            }

            PolicyConditionSlot? slot = null;
            var overflowed = false;
            var targetGateEligible = !IsPolicyRequirementFeed(
                coordinate,
                planningSession.RequirementFeedPaths);
            for (var pathIndex = 0; pathIndex < coordinate.Path.Length; pathIndex++)
            {
                if (coordinate.Path[pathIndex].Kind is SelectionPathSegmentKind.InlineFragment)
                {
                    targetGateEligible = false;
                    break;
                }
            }

            foreach (var occurrence in policyTarget.Occurrences)
            {
                var guardMask = planningSession.IncludeConditions.CreateGuardMask(occurrence.Conditions);

                if (!slots.TryGetOrAdd(
                    slotApplications.ToImmutable(),
                    slotApplicationClasses.ToImmutable(),
                    rmax,
                    new PolicyConditionCoordinate
                    {
                        TypeName = coordinate.TypeName,
                        FieldName = policyTarget.FieldName,
                        ResponseNames = coordinate.Kind is PolicyTargetKind.Field
                            ? [coordinate.Path.Name!]
                            : [],
                        Applications = [],
                        IsRoot = coordinate.Kind is PolicyTargetKind.Object && coordinate.Path.IsRoot,
                        LiveGuardMasks = [guardMask],
                        GateGuardMasks = occurrence.GateEligible && targetGateEligible
                            ? [guardMask]
                            : []
                    },
                    out slot,
                    out var updatedSlots))
                {
                    slots = updatedSlots;
                    overflowed = true;
                    break;
                }

                slots = updatedSlots;
            }

            if (overflowed)
            {
                AddResidualTargets(coordinate, policyTarget.Occurrences, condition: null);
                continue;
            }

            var condition = new ExecutionNodeCondition
            {
                VariableName = slot!.VariableName,
                PassingValue = true
            };

            if (targetGateEligible
                && policyTarget.Occurrences.Any(occurrence => occurrence.GateEligible))
            {
                gates.Add(new PolicyGate(coordinate.Kind, coordinate.Path, condition));
            }

            if (hasResidual)
            {
                AddResidualTargets(
                    coordinate with { Policies = residualApplications.ToArray() },
                    policyTarget.Occurrences,
                    condition);
            }
        }

        return (residualTargets.ToImmutable(), gates.ToImmutable(), slots);

        void AddSelectionSetObjectPolicy(
            FusionObjectTypeDefinition objectType,
            bool gateEligible)
        {
            if (objectType.PolicyApplications.IsDefaultOrEmpty)
            {
                return;
            }

            var target = AddPolicyRequirements(
                new PolicyExecutionTarget
                {
                    Kind = PolicyTargetKind.Object,
                    Path = selectionSet.Path,
                    TypeName = objectType.Name,
                    Policies = objectType.PolicyApplications.ToArray(),
                    Conditions = conditions
                },
                planningSession.Policies);
            coordinates.Add(
                new ConditionalPolicyExecutionTarget(
                    target,
                    [new PolicyTargetOccurrence(conditions, gateEligible)],
                    FieldName: null));
        }

        void AddResidualTargets(
            PolicyExecutionTarget target,
            ImmutableArray<PolicyTargetOccurrence> occurrences,
            ExecutionNodeCondition? condition)
        {
            List<ExecutionNodeCondition> commonConditions = occurrences.IsDefaultOrEmpty
                ? []
                : occurrences[0].Conditions.ToList();
            for (var i = commonConditions.Count - 1; i >= 0; i--)
            {
                for (var j = 1; j < occurrences.Length; j++)
                {
                    if (!occurrences[j].Conditions.Contains(commonConditions[i]))
                    {
                        commonConditions.RemoveAt(i);
                        break;
                    }
                }
            }

            if (condition is not null && !commonConditions.Contains(condition))
            {
                commonConditions.Add(condition);
            }

            residualTargets.Add(target with { Conditions = [.. commonConditions] });
        }

        static PolicyDenialBehavior Max(
            PolicyDenialBehavior left,
            PolicyDenialBehavior right)
            => left >= right ? left : right;
    }

    private static PolicyGateApplication CreatePolicyGateApplication(
        PolicyApplication application,
        PolicyPlanningState policyState)
        => new(
            PolicyNameGroups.Canonicalize(
                [.. application.Groups.Select(group => group
                    .Where(name => IsPolicyRequestCacheable(name, policyState))
                    .ToImmutableArray())]),
            application.OnDenied);

    private ImmutableStack<ConditionalPolicyExecutionTarget> CollectSpecializedPolicyTargets(
        SelectionSet selectionSet,
        ExecutionNodeCondition[] inheritedConditions)
    {
        var targets = new List<ConditionalPolicyExecutionTarget>();
        var path = selectionSet.Path;
        VisitSelectionSet(
            selectionSet.Node,
            selectionSet.Type,
            inheritedConditions,
            path,
            selectionSet.Type is FusionObjectTypeDefinition);

        var stack = ImmutableStack<ConditionalPolicyExecutionTarget>.Empty;
        for (var i = 0; i < targets.Count; i++)
        {
            stack = stack.Push(targets[i]);
        }

        return stack;

        void VisitSelectionSet(
            SelectionSetNode node,
            ITypeDefinition type,
            ExecutionNodeCondition[] conditions,
            SelectionPath currentPath,
            bool guaranteedConcrete)
        {
            if (type is not FusionComplexTypeDefinition
                && type is not FusionUnionTypeDefinition)
            {
                return;
            }

            var complexType = type as FusionComplexTypeDefinition;

            foreach (var selection in node.Selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (complexType is null
                            || !complexType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            allowInaccessibleFields: true,
                            out var field))
                        {
                            continue;
                        }

                        var fieldConditions = AddConditions(conditions, fieldNode.Directives);
                        var fieldPath = currentPath.AppendField(
                            fieldNode.Alias?.Value ?? fieldNode.Name.Value);

                        AddTarget(
                            PolicyTargetKind.Field,
                            fieldPath,
                            complexType.Name,
                            field.Name,
                            field.PolicyApplications,
                            fieldConditions,
                            gateEligible: guaranteedConcrete);

                        var fieldType = field.Type.NamedType();
                        var fieldTypeIsConcrete = fieldType is FusionObjectTypeDefinition;
                        AddObjectTargets(
                            fieldType,
                            fieldPath,
                            fieldConditions,
                            fieldTypeIsConcrete);

                        if (fieldNode.SelectionSet is { } childSelectionSet)
                        {
                            VisitSelectionSet(
                                childSelectionSet,
                                fieldType,
                                fieldConditions,
                                fieldPath,
                                fieldTypeIsConcrete);
                        }
                        break;

                    case InlineFragmentNode fragment:
                        var fragmentConditions = AddConditions(conditions, fragment.Directives);
                        var fragmentType = fragment.TypeCondition is null
                            ? type
                            : _schema.Types.GetType(
                                fragment.TypeCondition.Name.Value,
                                allowInaccessibleFields: true);
                        var fragmentPath = fragment.TypeCondition is null
                            ? currentPath
                            : currentPath.AppendFragment(fragment.TypeCondition.Name.Value);
                        var fragmentIsGuaranteedConcrete = guaranteedConcrete
                            && fragmentType is FusionObjectTypeDefinition;

                        AddObjectTargets(
                            fragmentType,
                            fragmentPath,
                            fragmentConditions,
                            fragmentIsGuaranteedConcrete);

                        VisitSelectionSet(
                            fragment.SelectionSet,
                            fragmentType,
                            fragmentConditions,
                            fragmentPath,
                            fragmentIsGuaranteedConcrete);
                        break;
                }
            }
        }

        void AddTarget(
            PolicyTargetKind kind,
            SelectionPath targetPath,
            string typeName,
            string? fieldName,
            ImmutableArray<PolicyApplication> applications,
            ExecutionNodeCondition[] conditions,
            bool gateEligible)
        {
            if (applications.IsDefaultOrEmpty)
            {
                return;
            }

            var target = new PolicyExecutionTarget
            {
                Kind = kind,
                Path = targetPath,
                TypeName = typeName,
                Policies = applications.ToArray(),
                Conditions = conditions
            };
            targets.Add(
                new ConditionalPolicyExecutionTarget(
                    target,
                    [new PolicyTargetOccurrence(conditions, gateEligible)],
                    fieldName));
        }

        void AddObjectTargets(
            ITypeDefinition type,
            SelectionPath targetPath,
            ExecutionNodeCondition[] targetConditions,
            bool guaranteedConcrete)
        {
            if (type is FusionObjectTypeDefinition objectType)
            {
                AddTarget(
                    PolicyTargetKind.Object,
                    targetPath,
                    objectType.Name,
                    fieldName: null,
                    objectType.PolicyApplications,
                    targetConditions,
                    gateEligible: guaranteedConcrete);
                return;
            }

            if (type is not FusionInterfaceTypeDefinition and not FusionUnionTypeDefinition)
            {
                return;
            }

            foreach (var possibleType in _schema.GetPossibleTypes(type, includeInaccessible: true))
            {
                AddTarget(
                    PolicyTargetKind.Object,
                    targetPath,
                    possibleType.Name,
                    fieldName: null,
                    possibleType.PolicyApplications,
                    targetConditions,
                    gateEligible: false);
            }
        }

        static ExecutionNodeCondition[] AddConditions(
            ExecutionNodeCondition[] inherited,
            IReadOnlyList<DirectiveNode> directives)
        {
            var own = ExtractConditions(directives);
            return own is null or { Count: 0 }
                ? inherited
                : [.. inherited, .. own];
        }
    }

    private static bool IsPolicyRequirementFeed(
        PolicyExecutionTarget coordinate,
        ImmutableHashSet<string> requirementFeedPaths)
    {
        var fields = new List<string>(coordinate.Path.Length);
        for (var i = 0; i < coordinate.Path.Length; i++)
        {
            if (coordinate.Path[i].Kind is SelectionPathSegmentKind.Field)
            {
                fields.Add(coordinate.Path[i].Name);
            }
        }

        var key = CreatePolicyPathKey(fields);
        if (coordinate.Kind is PolicyTargetKind.Field)
        {
            return requirementFeedPaths.Contains(key);
        }

        return requirementFeedPaths.Any(path =>
            key.Length == 0
            || path.Equals(key, StringComparison.Ordinal)
            || path.StartsWith(key + '\u001f', StringComparison.Ordinal));
    }

    private static string CreatePolicyPathKey(IEnumerable<string> fields)
        => string.Join('\u001f', fields);

    private readonly record struct PolicyGate(
        PolicyTargetKind Kind,
        SelectionPath Path,
        ExecutionNodeCondition Condition);

    private static (
        SelectionSetNode Resolvable,
        ImmutableStack<ConditionedSelectionSet> Unresolvable,
        ImmutableStack<ConditionedFieldSelection> FieldsWithRequirements,
        ISelectionSetIndex Index,
        ExecutionNodeCondition[] NodeConditions) ApplyPolicyGates(
        SelectionSet selectionSet,
        SelectionSetNode resolvable,
        ImmutableStack<ConditionedSelectionSet> unresolvable,
        ImmutableStack<ConditionedFieldSelection> fieldsWithRequirements,
        ISelectionSetIndex index,
        ImmutableArray<PolicyGate> gates)
    {
        if (gates.IsDefaultOrEmpty)
        {
            return (resolvable, unresolvable, fieldsWithRequirements, index, []);
        }

        var nodeConditions = gates
            .Where(gate => gate.Path == selectionSet.Path)
            .Select(gate => gate.Condition)
            .Distinct()
            .OrderBy(condition => condition.VariableName, StringComparer.Ordinal)
            .ToArray();
        var indexBuilder = index.ToBuilder();
        if (!indexBuilder.IsRegistered(resolvable))
        {
            indexBuilder.Register(selectionSet.Id, resolvable);
        }
        var rewritten = RewriteSelectionSet(resolvable, selectionSet.Path, indexBuilder, gates);
        var rewrittenUnresolvable = RewriteUnresolvable(unresolvable, gates);
        var rewrittenRequirements = RewriteRequirements(fieldsWithRequirements, gates);

        return (
            rewritten,
            rewrittenUnresolvable,
            rewrittenRequirements,
            indexBuilder,
            nodeConditions);

        static SelectionSetNode RewriteSelectionSet(
            SelectionSetNode source,
            SelectionPath currentPath,
            SelectionSetIndexBuilder indexBuilder,
            ImmutableArray<PolicyGate> gates)
        {
            List<ISelectionNode>? rewritten = null;

            for (var i = 0; i < source.Selections.Count; i++)
            {
                var selection = source.Selections[i];
                var replacement = selection;

                switch (selection)
                {
                    case FieldNode field:
                    {
                        var fieldPath = currentPath.AppendField(field.Alias?.Value ?? field.Name.Value);
                        var updatedField = field;

                        if (field.SelectionSet is { } childSelectionSet)
                        {
                            var rewrittenChild = RewriteSelectionSet(
                                childSelectionSet,
                                fieldPath,
                                indexBuilder,
                                gates);
                            var objectConditions = GetConditions(gates, fieldPath, PolicyTargetKind.Object);

                            if (objectConditions.Length > 0)
                            {
                                rewrittenChild = WrapSelectionSet(
                                    childSelectionSet,
                                    rewrittenChild,
                                    objectConditions,
                                    indexBuilder);
                            }
                            else if (!ReferenceEquals(rewrittenChild, childSelectionSet))
                            {
                                indexBuilder.Register(childSelectionSet, rewrittenChild);
                            }

                            if (!ReferenceEquals(rewrittenChild, childSelectionSet))
                            {
                                updatedField = field.WithSelectionSet(rewrittenChild);
                            }
                        }

                        var fieldConditions = GetConditions(gates, fieldPath, PolicyTargetKind.Field);
                        replacement = fieldConditions.Length == 0
                            ? updatedField
                            : WrapSelection(updatedField, fieldConditions, indexBuilder);
                        break;
                    }

                    case InlineFragmentNode fragment:
                    {
                        var fragmentPath = fragment.TypeCondition is null
                            ? currentPath
                            : currentPath.AppendFragment(fragment.TypeCondition.Name.Value);
                        var rewrittenChild = RewriteSelectionSet(
                            fragment.SelectionSet,
                            fragmentPath,
                            indexBuilder,
                            gates);

                        if (!ReferenceEquals(rewrittenChild, fragment.SelectionSet))
                        {
                            indexBuilder.Register(fragment.SelectionSet, rewrittenChild);
                            replacement = fragment.WithSelectionSet(rewrittenChild);
                        }

                        break;
                    }
                }

                if (!ReferenceEquals(selection, replacement))
                {
                    rewritten ??= [.. source.Selections.Take(i)];
                }

                rewritten?.Add(replacement);
            }

            if (rewritten is null)
            {
                return source;
            }

            var result = source.WithSelections(rewritten);
            indexBuilder.Register(source, result);
            return result;
        }

        static SelectionSetNode WrapSelectionSet(
            SelectionSetNode original,
            SelectionSetNode selectionSet,
            ExecutionNodeCondition[] conditions,
            SelectionSetIndexBuilder indexBuilder)
        {
            var inner = new SelectionSetNode(selectionSet.Selections);
            indexBuilder.Register(inner);
            var current = inner;

            for (var i = conditions.Length - 1; i >= 0; i--)
            {
                var wrapper = new SelectionSetNode([
                    new InlineFragmentNode(
                        null,
                        null,
                        [CreateIncludeDirective(conditions[i])],
                        current)
                ]);

                if (i == 0)
                {
                    indexBuilder.Register(original, wrapper);
                }
                else
                {
                    indexBuilder.Register(wrapper);
                }

                current = wrapper;
            }

            return current;
        }

        static ISelectionNode WrapSelection(
            ISelectionNode selection,
            ExecutionNodeCondition[] conditions,
            SelectionSetIndexBuilder indexBuilder)
        {
            var current = selection;

            for (var i = conditions.Length - 1; i >= 0; i--)
            {
                var selectionSet = new SelectionSetNode([current]);
                indexBuilder.Register(selectionSet);
                current = new InlineFragmentNode(
                    null,
                    null,
                    [CreateIncludeDirective(conditions[i])],
                    selectionSet);
            }

            return current;
        }

        static DirectiveNode CreateIncludeDirective(ExecutionNodeCondition condition)
            => new(
                null,
                new NameNode(DirectiveNames.Include.Name),
                [
                    new ArgumentNode(
                        null,
                        new NameNode("if"),
                        new VariableNode(new NameNode(condition.VariableName)))
                ]);

        static ExecutionNodeCondition[] GetConditions(
            ImmutableArray<PolicyGate> gates,
            SelectionPath path,
            PolicyTargetKind kind)
            => gates
                .Where(gate => gate.Kind == kind && gate.Path == path)
                .Select(gate => gate.Condition)
                .Distinct()
                .OrderBy(condition => condition.VariableName, StringComparer.Ordinal)
                .ToArray();

        static ImmutableStack<ConditionedSelectionSet> RewriteUnresolvable(
            ImmutableStack<ConditionedSelectionSet> entries,
            ImmutableArray<PolicyGate> gates)
        {
            if (entries.IsEmpty)
            {
                return entries;
            }

            var source = entries.ToArray();
            var result = ImmutableStack<ConditionedSelectionSet>.Empty;

            for (var i = source.Length - 1; i >= 0; i--)
            {
                var entry = source[i];
                var conditions = GetBacklogConditions(entry.SelectionSet.Path, entry.Conditions, gates);
                result = result.Push(entry with { Conditions = conditions });
            }

            return result;
        }

        static ImmutableStack<ConditionedFieldSelection> RewriteRequirements(
            ImmutableStack<ConditionedFieldSelection> entries,
            ImmutableArray<PolicyGate> gates)
        {
            if (entries.IsEmpty)
            {
                return entries;
            }

            var source = entries.ToArray();
            var result = ImmutableStack<ConditionedFieldSelection>.Empty;

            for (var i = source.Length - 1; i >= 0; i--)
            {
                var entry = source[i];
                var field = entry.FieldSelection;
                var path = field.Path.AppendField(field.Node.Alias?.Value ?? field.Node.Name.Value);
                var conditions = GetBacklogConditions(path, entry.Conditions, gates);
                result = result.Push(entry with { Conditions = conditions });
            }

            return result;
        }

        static ExecutionNodeCondition[] GetBacklogConditions(
            SelectionPath path,
            ExecutionNodeCondition[] conditions,
            ImmutableArray<PolicyGate> gates)
        {
            List<ExecutionNodeCondition>? result = null;

            foreach (var gate in gates)
            {
                if (!gate.Path.IsParentOfOrSame(path)
                    || conditions.Contains(gate.Condition))
                {
                    continue;
                }

                result ??= [.. conditions];

                if (!result.Contains(gate.Condition))
                {
                    result.Add(gate.Condition);
                }
            }

            return result is null
                ? conditions
                : [.. result
                    .OrderBy(condition => condition.VariableName, StringComparer.Ordinal)
                    .ThenBy(condition => condition.PassingValue)];
        }
    }

    private static PolicyExecutionTarget AddPolicyRequirements(
        PolicyExecutionTarget target,
        PolicyPlanningState policyState)
    {
        List<PolicyRequirement>? requirements = null;
        HashSet<string>? seenNames = null;

        foreach (var application in target.Policies)
        {
            foreach (var group in application.Groups)
            {
                foreach (var name in group)
                {
                    seenNames ??= new HashSet<string>(StringComparer.Ordinal);

                    if (!seenNames.Add(name)
                        || !policyState.Requirements.TryGetValue(name, out var selectionSet))
                    {
                        continue;
                    }

                    requirements ??= [];
                    requirements.Add(new PolicyRequirement
                    {
                        PolicyName = name,
                        SelectionSet = selectionSet
                    });
                }
            }
        }

        return requirements is null
            ? target
            : target with { Requirements = requirements.ToArray() };
    }

    private bool IsQueryRootSelection(SelectionSet selectionSet)
        => selectionSet.Type.Name.Equals(_schema.QueryType.Name, StringComparison.Ordinal);

    private PlanNode InlineLookupRequirements(
        SelectionSet workItemSelectionSet,
        PlanNode current,
        Lookup lookup,
        int lookupStepDepth,
        Backlog backlog,
        ExecutionNodeCondition[]? conditions,
        bool allowSourceSchemaReentry,
        SourceSchemaNodePlanningPolicy? sourceSchemaNodePolicy,
        out SelectionSetNode? unresolvedRequirements)
    {
        var processed = new HashSet<string>();
        var lookupStepId = current.Steps.NextId();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.Requirements;
        var descendantPolicy = sourceSchemaNodePolicy is null
            ? null
            : SourceSchemaNodePlanningPolicy.Descendant;

        if (index.IsRegistered(selectionSet))
        {
            // if we already used the same requirements object and have indexed it
            // we will do a deep clone of the syntax tree so that the planner does
            // not get confused and sees them as different requirements.
            selectionSet = SyntaxRewriter.Create(
                node =>
                {
                    if (node is FieldNode { SelectionSet: null } leafField)
                    {
                        return new FieldNode(
                            leafField.Name,
                            leafField.Alias,
                            leafField.Directives,
                            leafField.Arguments,
                            leafField.SelectionSet);
                    }

                    return node;
                }).Rewrite(selectionSet)!;
        }

        index.Register(workItemSelectionSet.Id, selectionSet);

        var internalOperation = InlineSelectionsIntoOverallOperation(
            current.InternalOperationDefinition,
            index,
            workItemSelectionSet.Type,
            workItemSelectionSet.Id,
            selectionSet);

        foreach (var (step, stepIndex, schemaName) in current.GetCandidateSteps(workItemSelectionSet.Id))
        {
            // A step can only carry a requirement that lives within the data it produces.
            // Match the candidate's response scope so a requirement cannot be inlined into
            // a step producing the same logical selection at a different path.
            if (!step.Target.IsParentOfOrSame(workItemSelectionSet.Path))
            {
                continue;
            }

            if (!processed.Add(schemaName)
                || (!allowSourceSchemaReentry && lookup.SchemaName.Equals(schemaName)))
            {
                continue;
            }

            var relativePath = workItemSelectionSet.Path.RelativeTo(step.Target);

            // A lookup key can already be present in this step because an ancestor @provides
            // scope selected the concrete runtime field. Match the actual operation path to
            // verify that this response scope already contains the requirement.
            if (ContainsSelectionsAtPath(
                step.Definition.SelectionSet,
                relativePath,
                selectionSet))
            {
                steps = steps.SetItem(
                    stepIndex,
                    step with { Dependents = step.Dependents.Add(lookupStepId) });
                selectionSet = null;
                break;
            }

            var input = new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                SelectionSet = workItemSelectionSet with { Node = selectionSet },
                SelectionSetIndex = index,
                TreatSourceExternalAsUnresolvable = sourceSchemaNodePolicy is not null
            };

            var (resolvable, unresolvable, _, _, _) = _partitioner.Partition(input);

            if (resolvable is { Selections.Count: > 0 })
            {
                var operation =
                    InlineSelections(
                        step.Definition,
                        index,
                        workItemSelectionSet.Type,
                        index.GetId(resolvable),
                        resolvable);

                var updatedStep = step with
                {
                    Definition = operation,

                    // we need to update the selection sets that this plan step
                    // has as the requirement could have introduced new ones.
                    SelectionSets = SelectionSetIndexer.CreateIdSet(operation.SelectionSet, index),

                    // we add the new lookup node to the dependents of the current step.
                    // the new lookup node will be the next index added which is the last index aka Count.
                    Dependents = step.Dependents.Add(lookupStepId)
                };

                steps = steps.SetItem(stepIndex, updatedStep);

                selectionSet = null;

                if (!unresolvable.IsEmpty)
                {
                    var top = unresolvable.Peek();
                    if (top.SelectionSet.Id == workItemSelectionSet.Id)
                    {
                        unresolvable = unresolvable.Pop(out top);
                        selectionSet = top.SelectionSet.Node;
                    }

                    backlog = backlog.PushUnresolvable(
                        unresolvable,
                        current.SchemaName,
                        GetOperationStepDepth(current, step.Id),
                        dependents: ImmutableHashSet<int>.Empty.Add(lookupStepId),
                        sourceSchemaNodePolicy: descendantPolicy);
                }
            }

            if (selectionSet is null)
            {
                break;
            }
        }

        // Fallback: if no candidate step was found via exact selection set ID match,
        // walk the internal operation AST to find the nearest ancestor step and the
        // complete connector chain to the target selection set.
        if (selectionSet is not null
            && TryFindAncestorStepForRequirement(
                internalOperation,
                steps,
                index,
                workItemSelectionSet.Id,
                workItemSelectionSet.Path) is { } ancestorMatch
            && processed.Add(ancestorMatch.Step.SchemaName!)
            && (allowSourceSchemaReentry || !lookup.SchemaName.Equals(ancestorMatch.Step.SchemaName)))
        {
            if (TryInlineIntoAncestorStep(
                ancestorMatch,
                selectionSet,
                workItemSelectionSet.Path,
                lookupStepId,
                index,
                ref steps,
                out var unresolvable,
                treatSourceExternalAsUnresolvable: sourceSchemaNodePolicy is not null))
            {
                selectionSet = null;

                if (!unresolvable.IsEmpty)
                {
                    var top = unresolvable.Peek();
                    if (top.SelectionSet.Id == workItemSelectionSet.Id)
                    {
                        unresolvable = unresolvable.Pop(out top);
                        selectionSet = top.SelectionSet.Node;
                    }

                    backlog = backlog.PushUnresolvable(
                        unresolvable,
                        current.SchemaName,
                        GetOperationStepDepth(current, ancestorMatch.Step.Id),
                        dependents: ImmutableHashSet<int>.Empty.Add(lookupStepId),
                        sourceSchemaNodePolicy: descendantPolicy);
                }
            }
        }

        unresolvedRequirements = selectionSet;

        // if we have still selections left we need to add them to the backlog. A nested
        // object/list leftover is re-rooted at its own entity type so a requirement that
        // crosses an entity boundary resolves via that entity's key lookup.
        if (selectionSet is not null)
        {
            backlog = PushRequirementLeftover(
                backlog,
                index,
                selectionSet,
                workItemSelectionSet.Id,
                workItemSelectionSet.Type,
                workItemSelectionSet.Path,
                lookup.SchemaName,
                ImmutableHashSet<int>.Empty.Add(lookupStepId),
                lookupStepDepth,
                conditions ?? [],
                sourceSchemaNodePolicy);
        }

        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                current.Options,
                current.MaxDepth,
                current.OpsPerLevel,
                backlog.Cost);

        return current with
        {
            Steps = steps,
            Backlog = backlog,
            RemainingCost = remainingCost,
            SelectionSetIndex = index,
            InternalOperationDefinition = internalOperation
        };
    }

    internal static bool ContainsSelectionsAtPath(
        SelectionSetNode current,
        SelectionPath path,
        SelectionSetNode required)
        => ContainsSelectionsAtPath(current, path, pathIndex: 0, required);

    private static bool ContainsSelectionsAtPath(
        SelectionSetNode current,
        SelectionPath path,
        int pathIndex,
        SelectionSetNode required)
    {
        if (pathIndex == path.Length)
        {
            return ContainsAllSelections(required, current);
        }

        var segment = path[pathIndex];

        foreach (var selection in current.Selections)
        {
            if (selection is InlineFragmentNode
                {
                    TypeCondition: null,
                    Directives.Count: 0
                } wrapper
                && ContainsSelectionsAtPath(wrapper.SelectionSet, path, pathIndex, required))
            {
                return true;
            }

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Field
                    when selection is FieldNode
                    {
                        SelectionSet: { } child,
                        Directives.Count: 0
                    } field && (field.Alias?.Value ?? field.Name.Value).Equals(segment.Name, StringComparison.Ordinal):
                    if (ContainsSelectionsAtPath(child, path, pathIndex + 1, required))
                    {
                        return true;
                    }
                    break;

                case SelectionPathSegmentKind.InlineFragment
                    when selection is InlineFragmentNode
                    {
                        TypeCondition.Name.Value: var typeName,
                        Directives.Count: 0
                    } fragment && typeName.Equals(segment.Name, StringComparison.Ordinal):
                    if (ContainsSelectionsAtPath(
                        fragment.SelectionSet,
                        path,
                        pathIndex + 1,
                        required))
                    {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static bool ContainsAllSelections(
        SelectionSetNode required,
        SelectionSetNode existing)
    {
        foreach (var requiredSelection in required.Selections)
        {
            var isCovered = requiredSelection switch
            {
                FieldNode requiredField => existing.Selections
                    .OfType<FieldNode>()
                    .Any(existingField => FieldContains(requiredField, existingField)),
                InlineFragmentNode requiredFragment => existing.Selections
                    .OfType<InlineFragmentNode>()
                    .Any(existingFragment => FragmentContains(requiredFragment, existingFragment)),
                _ => false
            };

            if (!isCovered)
            {
                return false;
            }
        }

        return true;
    }

    private static bool FieldContains(FieldNode required, FieldNode existing)
    {
        if (!SyntaxComparer.BySyntax.Equals(required.Name, existing.Name)
            || !SyntaxComparer.BySyntax.Equals(required.Alias, existing.Alias)
            || !required.Arguments.SequenceEqual(existing.Arguments, SyntaxComparer.BySyntax)
            || !required.Directives.SequenceEqual(existing.Directives, SyntaxComparer.BySyntax))
        {
            return false;
        }

        return required.SelectionSet is null
            || (existing.SelectionSet is not null
                && ContainsAllSelections(required.SelectionSet, existing.SelectionSet));
    }

    private static bool FragmentContains(InlineFragmentNode required, InlineFragmentNode existing)
        => SyntaxComparer.BySyntax.Equals(required.TypeCondition, existing.TypeCondition)
            && required.Directives.SequenceEqual(existing.Directives, SyntaxComparer.BySyntax)
            && ContainsAllSelections(required.SelectionSet, existing.SelectionSet);

    /// <summary>
    /// Tries to inline the field that has a requirement into its original intended plan step
    /// by resolving the requirements from non-dependant siblings or from parents.
    /// </summary>
    private void PlanInlineFieldWithRequirements(
        FieldRequirementWorkItem workItem,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog,
        PolicyPlanningSession planningSession)
    {
        // The main planning backlog handles step-owned requirements. Incremental
        // plan requirements are handled by defer planning.
        if (workItem.Consumer is not StepConsumer stepConsumer)
        {
            return;
        }

        // we first resolve the original intended plan step, so we can inline the field
        // into it.
        if (current.Steps.ById(stepConsumer.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        // next we try to inline the requirements into non-dependant siblings or parents.
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var requirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{requirementId}";

        var success =
            TryInlineFieldRequirements(
                    workItem,
                    stepConsumer.StepId,
                    ref current,
                    currentStep,
                    index,
                    ref backlog,
                    ref steps,
                    out var requirementAliases)
                is null;

        if (!success)
        {
            return;
        }

        var compositeField = workItem.Selection.Field;
        var sourceField = compositeField.Sources[current.SchemaName];
        var arguments = new List<ArgumentNode>(workItem.Selection.Node.Arguments);

        foreach (var argument in sourceField.Requirements!.Arguments)
        {
            // arguments that are exposed on the composite schema
            // are not requirements, and we can skip them.
            if (compositeField.Arguments.ContainsName(argument.Name, allowInaccessibleFields: true))
            {
                continue;
            }

            arguments.Add(
                new ArgumentNode(
                    new NameNode(argument.Name),
                    new VariableNode(new NameNode($"{requirementKey}_{argument.Name}"))));
        }

        var specializedField = workItem.Selection.Node.WithArguments(arguments);
        var specializedSelectionSet = new SelectionSet(
            workItem.Selection.SelectionSetId,
            new SelectionSetNode([specializedField]),
            compositeField.DeclaringType,
            workItem.Selection.Path);
        var specializedTargets = CollectSpecializedPolicyTargets(
            specializedSelectionSet,
            workItem.Conditions);
        var (policyTargets, policyGates, policySlots) = BuildPolicyTargets(
            specializedTargets,
            specializedSelectionSet,
            workItem.Conditions,
            includeSelectionSetObjectPolicy: false,
            selectionSetGateEligible: false,
            current.PolicySlots,
            planningSession);

        // we partition the requiring field's children against the consumer schema and
        // only inline the resolvable subset. Unresolvable descendants and nested
        // fields-with-requirements are pushed onto the backlog so that they are planned
        // as re-entrant lookups instead of being inlined into a step that cannot resolve
        // them.
        var childSelections =
            ExtractResolvableChildSelections(
                stepConsumer.StepId,
                workItem.EstimatedDepth,
                workItem.Selection,
                current,
                index,
                ref backlog,
                workItem.SourceSchemaNodePolicy,
                policyGates);

        var inlinedSelectionSet = new SelectionSetNode(
            [specializedField.WithSelectionSet(childSelections)]);
        (inlinedSelectionSet, _, _, _, var slotConditions) = ApplyPolicyGates(
            specializedSelectionSet with { Node = inlinedSelectionSet },
            inlinedSelectionSet,
            [],
            [],
            index,
            policyGates);

        var operation =
            InlineSelections(
                currentStep.Definition,
                index,
                currentStep.Type,
                workItem.Selection.SelectionSetId,
                inlinedSelectionSet);

        var requirements = currentStep.Requirements;

        for (var i = 0; i < sourceField.Requirements.Arguments.Length; i++)
        {
            var argument = sourceField.Requirements.Arguments[i];
            var fieldSelectionMap = sourceField.Requirements.Fields[i];

            if (fieldSelectionMap is null)
            {
                continue;
            }

            var argumentRequirementKey = $"{requirementKey}_{argument.Name}";
            var operationRequirement = new OperationRequirement(
                argumentRequirementKey,
                argument.Type,
                workItem.Selection.Path,
                fieldSelectionMap,
                requirementAliases.GetInternalAlias(fieldSelectionMap));

            requirements = requirements.Add(argumentRequirementKey, operationRequirement);
        }

        var policyStepId = policyTargets.IsDefaultOrEmpty ? 0 : steps.NextId();
        var updatedStep = currentStep with
        {
            Definition = operation,
            SelectionSets = SelectionSetIndexer.CreateIdSet(operation.SelectionSet, index),
            Requirements = requirements,
            Dependents = policyStepId == 0
                ? currentStep.Dependents
                : currentStep.Dependents.Add(policyStepId),
            Conditions = slotConditions.Length == 0
                ? currentStep.Conditions
                : [.. currentStep.Conditions, .. slotConditions]
        };

        steps = steps.SetItem(stepConsumer.StepId - 1, updatedStep);
        if (policyStepId != 0)
        {
            steps = steps.Add(new PolicyPlanStep
            {
                Id = policyStepId,
                Targets = policyTargets
            });
        }

        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                current.Options,
                current.MaxDepth,
                current.OpsPerLevel,
                backlog.Cost);

        var next = new PlanNode
        {
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            RemainingCost = remainingCost,
            Steps = steps,
            LastRequirementId = requirementId,
            RequirementAliases = requirementAliases.Registry,
            PolicySlots = policySlots,
            OperationStepCount = current.OperationStepCount,
            MaxDepth = current.MaxDepth,
            ExcessFanout = current.ExcessFanout,
            OpsPerLevel = current.OpsPerLevel,
            OperationStepDepths = current.OperationStepDepths,
            EventStreamDirective = current.EventStreamDirective
        };

        possiblePlans.EnqueueBranches(next);
    }

    private void PlanFieldWithRequirement(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog,
        PolicyPlanningSession planningSession)
    {
        // The main planning backlog handles step-owned requirements. Incremental
        // plan requirements are handled by defer planning.
        if (workItem.Consumer is not StepConsumer stepConsumer)
        {
            return;
        }

        if (current.Steps.ById(stepConsumer.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        var mergeWithExistingStep =
            TryFindMergeableRequirementLookupStep(
                current,
                workItem,
                lookup,
                stepConsumer.StepId,
                out var existingStep,
                out var existingStepIndex);

        if (!mergeWithExistingStep)
        {
            var selectionSetStub = new SelectionSet(
                workItem.Selection.SelectionSetId,
                new SelectionSetNode([workItem.Selection.Node]),
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.Path);
            current = InlineLookupRequirements(
                selectionSetStub,
                current,
                lookup,
                workItem.EstimatedDepth,
                backlog,
                workItem.Conditions,
                allowSourceSchemaReentry: false,
                workItem.SourceSchemaNodePolicy,
                out _);
            backlog = current.Backlog;

            if (current.Steps.ById(stepConsumer.StepId) is not OperationPlanStep updatedCurrentStep)
            {
                return;
            }

            currentStep = updatedCurrentStep;
        }

        var steps = current.Steps;
        var stepId = mergeWithExistingStep ? existingStep.Id : current.Steps.NextId();
        var stepDepth = mergeWithExistingStep
            ? GetOperationStepDepth(current, existingStep.Id)
            : workItem.EstimatedDepth;
        var indexBuilder = current.SelectionSetIndex.ToBuilder();
        var lastRequirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{lastRequirementId}";

        var leftoverRequirements =
            TryInlineFieldRequirements(
                workItem,
                stepConsumer.StepId,
                ref current,
                currentStep,
                indexBuilder,
                ref backlog,
                ref steps,
                out var requirementAliases);

        // if we have requirements that we could not inline into existing nodes of the
        // operation plan we will put them on the backlog to be planned as another lookup.
        // A nested object/list leftover is re-rooted at its own entity type so a requirement
        // that crosses an entity boundary resolves via that entity's key lookup.
        if (leftoverRequirements is not null)
        {
            backlog = PushRequirementLeftover(
                backlog,
                indexBuilder,
                leftoverRequirements,
                workItem.Selection.SelectionSetId,
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.Path,
                lookup.SchemaName,
                ImmutableHashSet<int>.Empty.Add(stepId),
                stepDepth,
                workItem.Conditions,
                workItem.SourceSchemaNodePolicy);
        }

        var compositeField = workItem.Selection.Field;
        var sourceField = compositeField.Sources[current.SchemaName];
        var requirements = mergeWithExistingStep
            ? existingStep.Requirements
            : ImmutableDictionary<string, OperationRequirement>.Empty;
        var arguments = new List<ArgumentNode>(workItem.Selection.Node.Arguments);

        for (var i = 0; i < sourceField.Requirements!.Arguments.Length; i++)
        {
            var argument = sourceField.Requirements.Arguments[i];
            var fieldSelectionMap = sourceField.Requirements.Fields[i];

            if (fieldSelectionMap is null)
            {
                continue;
            }

            var argumentRequirementKey = $"{requirementKey}_{argument.Name}";
            var operationRequirement = new OperationRequirement(
                argumentRequirementKey,
                argument.Type,
                workItem.Selection.Path,
                fieldSelectionMap,
                requirementAliases.GetInternalAlias(fieldSelectionMap));

            requirements = requirements.Add(argumentRequirementKey, operationRequirement);
        }

        foreach (var argument in sourceField.Requirements!.Arguments)
        {
            // arguments that are exposed on the composite schema
            // are not requirements, and we can skip them.
            if (compositeField.Arguments.ContainsName(argument.Name, allowInaccessibleFields: true))
            {
                continue;
            }

            arguments.Add(
                new ArgumentNode(
                    new NameNode(argument.Name),
                    new VariableNode(new NameNode($"{requirementKey}_{argument.Name}"))));
        }

        var specializedField = workItem.Selection.Node.WithArguments(arguments);
        var specializedSelectionSet = new SelectionSet(
            workItem.Selection.SelectionSetId,
            new SelectionSetNode([specializedField]),
            compositeField.DeclaringType,
            workItem.Selection.Path);
        var specializedTargets = CollectSpecializedPolicyTargets(
            specializedSelectionSet,
            workItem.Conditions);
        var (policyTargets, policyGates, policySlots) = BuildPolicyTargets(
            specializedTargets,
            specializedSelectionSet,
            workItem.Conditions,
            includeSelectionSetObjectPolicy: false,
            selectionSetGateEligible: false,
            current.PolicySlots,
            planningSession);

        var childSelections =
            ExtractResolvableChildSelections(
                stepId,
                stepDepth,
                workItem.Selection,
                current,
                indexBuilder,
                ref backlog,
                workItem.SourceSchemaNodePolicy,
                policyGates);

        var selectionSetNode = new SelectionSetNode(
            [specializedField.WithSelectionSet(childSelections)]);
        (selectionSetNode, _, _, _, var slotConditions) = ApplyPolicyGates(
            specializedSelectionSet with { Node = selectionSetNode },
            selectionSetNode,
            [],
            [],
            indexBuilder,
            policyGates);
        indexBuilder.Register(workItem.Selection.SelectionSetId, selectionSetNode);

        if (mergeWithExistingStep)
        {
            if (steps[existingStepIndex] is not OperationPlanStep refreshedExistingStep)
            {
                return;
            }

            var operation = InlineSelections(
                refreshedExistingStep.Definition,
                indexBuilder,
                compositeField.DeclaringType,
                refreshedExistingStep.RootSelectionSetId,
                selectionSetNode);
            EnsureAllSelectionSetsRegistered(operation.SelectionSet, indexBuilder);

            var policyStepId = policyTargets.IsDefaultOrEmpty ? 0 : steps.NextId();
            var updatedStep = refreshedExistingStep with
            {
                Definition = operation,
                SelectionSets = SelectionSetIndexer.CreateIdSet(operation.SelectionSet, indexBuilder),
                Requirements = requirements,
                Dependents = policyStepId == 0
                    ? refreshedExistingStep.Dependents
                    : refreshedExistingStep.Dependents.Add(policyStepId),
                Conditions = slotConditions.Length == 0
                    ? refreshedExistingStep.Conditions
                    : [.. refreshedExistingStep.Conditions, .. slotConditions]
            };

            steps = steps.SetItem(existingStepIndex, updatedStep);
            if (policyStepId != 0)
            {
                steps = steps.Add(new PolicyPlanStep
                {
                    Id = policyStepId,
                    Targets = policyTargets
                });
            }

            var mergeRemainingCost =
                PlannerCostEstimator.EstimateRemainingCost(
                    current.Options,
                    current.MaxDepth,
                    current.OpsPerLevel,
                    backlog.Cost);

            var mergeNext = new PlanNode
            {
                OperationDefinition = current.OperationDefinition,
                InternalOperationDefinition = current.InternalOperationDefinition,
                ShortHash = current.ShortHash,
                SchemaName = current.SchemaName,
                Options = current.Options,
                SelectionSetIndex = indexBuilder,
                Backlog = backlog,
                RemainingCost = mergeRemainingCost,
                Steps = steps,
                LastRequirementId = lastRequirementId,
                RequirementAliases = requirementAliases.Registry,
                PolicySlots = policySlots,
                OperationStepCount = current.OperationStepCount,
                MaxDepth = current.MaxDepth,
                ExcessFanout = current.ExcessFanout,
                OpsPerLevel = current.OpsPerLevel,
                OperationStepDepths = current.OperationStepDepths,
                EventStreamDirective = current.EventStreamDirective
            };

            possiblePlans.EnqueueBranches(mergeNext);
            return;
        }

        var operationBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetName(current.CreateOperationName(stepId))
                .SetSelectionSet(selectionSetNode);

        lastRequirementId++;
        requirementKey = $"__fusion_{lastRequirementId}";

        for (var i = 0; i < workItem.Lookup!.Arguments.Length; i++)
        {
            var argument = workItem.Lookup.Arguments[i];
            var fieldSelectionMap = workItem.Lookup.Fields[i];

            var argumentRequirementKey = $"{requirementKey}_{argument.Name}";
            var operationRequirement = new OperationRequirement(
                argumentRequirementKey,
                argument.Type,
                workItem.Selection.Path,
                fieldSelectionMap,
                null);

            requirements = requirements.Add(argumentRequirementKey, operationRequirement);
        }

        operationBuilder.SetLookup(
            lookup,
            GetLookupArguments(lookup, requirementKey),
            workItem.Selection.Field.DeclaringType);

        var (definition, index, source) = operationBuilder.Build(indexBuilder);

        var newPolicyStepId = policyTargets.IsDefaultOrEmpty ? 0 : stepId + 1;
        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = compositeField.DeclaringType,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(selectionSetNode),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, indexBuilder),
            Requirements = requirements,
            Dependents = newPolicyStepId == 0
                ? currentStep.Dependents
                : currentStep.Dependents.Add(newPolicyStepId),
            Conditions = slotConditions.Length == 0
                ? workItem.Conditions
                : [.. workItem.Conditions, .. slotConditions],
            Target = workItem.Selection.Path,
            Source = source,
            Lookup = lookup
        };

        var costState = AddOperationStepCostState(current, stepId, stepDepth);
        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlog.Cost);

        steps = steps.Add(step);
        if (newPolicyStepId != 0)
        {
            steps = steps.Add(new PolicyPlanStep
            {
                Id = newPolicyStepId,
                Targets = policyTargets
            });
        }

        var next = new PlanNode
        {
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            RemainingCost = remainingCost,
            Steps = steps,
            LastRequirementId = lastRequirementId,
            RequirementAliases = requirementAliases.Registry,
            PolicySlots = policySlots,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths,
            EventStreamDirective = current.EventStreamDirective
        };

        possiblePlans.EnqueueBranches(next);
    }

    private static bool TryFindMergeableRequirementLookupStep(
        PlanNode current,
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        int consumerStepId,
        out OperationPlanStep step,
        out int stepIndex)
    {
        for (var i = 0; i < current.Steps.Count; i++)
        {
            if (current.Steps[i] is OperationPlanStep candidate
                && candidate.Id != consumerStepId
                && candidate.RootSelectionSetId == workItem.Selection.SelectionSetId
                && candidate.Target == workItem.Selection.Path
                && candidate.SchemaName?.Equals(current.SchemaName, StringComparison.Ordinal) == true
                && ReferenceEquals(candidate.Lookup, lookup)
                && candidate.Type.Name.Equals(
                    workItem.Selection.Field.DeclaringType.Name,
                    StringComparison.Ordinal)
                && ConditionsEqual(candidate.Conditions, workItem.Conditions))
            {
                step = candidate;
                stepIndex = i;
                return true;
            }
        }

        step = null!;
        stepIndex = -1;
        return false;
    }

    private static bool ConditionsEqual(
        ExecutionNodeCondition[] left,
        ExecutionNodeCondition[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (!left[i].Equals(right[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void PlanNodeLookup(
        NodeLookupWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog,
        PolicyPlanningSession planningSession)
    {
        var stepId = current.Steps.NextId();
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = workItem.SelectionSet,
            SelectionSetIndex = index,
            Conditions = workItem.Conditions,
            TreatSourceExternalAsUnresolvable = workItem.SourceSchemaNodePolicy is not null
        };

        (var resolvable, var unresolvable, var fieldsWithRequirements, var policyTargets, index) =
            _partitioner.Partition(input);

        if (resolvable is not { Selections.Count: > 0 }
            && workItem.SourceSchemaNodePolicy is { } sourceSchemaNodePolicy)
        {
            if (!CanSeedSourceSchemaNodeBranch(
                current.SchemaName,
                fieldsWithRequirements,
                unresolvable,
                sourceSchemaNodePolicy,
                index))
            {
                return;
            }

            resolvable = new SelectionSetNode([new FieldNode(IntrospectionFieldNames.TypeName)]);
            var strictIndexBuilder = index.ToBuilder();
            strictIndexBuilder.Register(workItem.SelectionSet.Id, resolvable);
            index = strictIndexBuilder;
        }

        // if we cannot resolve any selection with the current source schema then this path
        // cannot be used to resolve the data for the current operation, and we need to skip it.
        if (resolvable is null
            || (workItem.SourceSchemaNodePolicy is not null
                && resolvable.Selections.Count == 0))
        {
            return;
        }

        var (targets, policyGates, policySlots) = BuildPolicyTargets(
            policyTargets,
            workItem.SelectionSet,
            workItem.Conditions,
            includeSelectionSetObjectPolicy: true,
            selectionSetGateEligible: !index.IsConcreteBranch(workItem.SelectionSet.Id),
            current.PolicySlots,
            planningSession);
        ExecutionNodeCondition[] slotConditions;

        (resolvable, unresolvable, fieldsWithRequirements, index, slotConditions) =
            ApplyPolicyGates(
                workItem.SelectionSet,
                resolvable,
                unresolvable,
                fieldsWithRequirements,
                index,
                policyGates);

        var descendantPolicy = workItem.SourceSchemaNodePolicy is null
            ? null
            : SourceSchemaNodePlanningPolicy.Descendant;
        backlog = backlog.PushUnresolvable(
            unresolvable,
            current.SchemaName,
            stepDepth,
            sourceSchemaNodePolicy: descendantPolicy);
        backlog = backlog.PushRequirements(
            fieldsWithRequirements,
            new StepConsumer(stepId),
            stepDepth,
            descendantPolicy);

        var resolvableSelections = resolvable.Selections;
        if (!resolvableSelections.Any(IsTypeNameSelection))
        {
            resolvableSelections = [new FieldNode(IntrospectionFieldNames.TypeName), .. resolvableSelections];
        }

        var selectionSetNode = resolvable.WithSelections(resolvableSelections);

        var indexBuilder = index.ToBuilder();
        indexBuilder.Register(workItem.SelectionSet.Id, selectionSetNode);

        if (lookup.Arguments.Length != 1)
        {
            throw new InvalidOperationException("Expected exactly one argument on node lookup");
        }

        var argument = new ArgumentNode(
            new NameNode(lookup.Arguments[0].Name),
            workItem.IdArgumentValue);

        var operationBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetName(current.CreateOperationName(stepId))
                .SetSelectionSet(selectionSetNode);

        operationBuilder.SetLookup(lookup, [argument], workItem.SelectionSet.Type, workItem.ResponseName);

        (var definition, index, _) = operationBuilder.Build(indexBuilder);

        var policyStepId = targets.IsDefaultOrEmpty ? 0 : stepId + 1;

        var operationPlanStep = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(resolvable),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Dependents = policyStepId == 0
                ? workItem.Dependents
                : workItem.Dependents.Add(policyStepId),
#if NET10_0_OR_GREATER
            Requirements = [],
#else
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
#endif
            Conditions = slotConditions,
            Target = SelectionPath.Root,
            Source = SelectionPath.Root,
            Lookup = lookup
        };

        var nodePlanStep = current.Steps.OfType<NodeFieldPlanStep>().LastOrDefault() ??
            throw new InvalidOperationException($"Expected to find a {nameof(NodeFieldPlanStep)} in the existing steps.");

        var steps = current.Steps;

        // Add a new branch to the existing node plan step
        steps = steps.Replace(nodePlanStep,
            nodePlanStep with
            {
                Branches = nodePlanStep.Branches.SetItem(workItem.SelectionSet.Type.Name, operationPlanStep)
            });

        // Add the lookup operation to the steps
        steps = steps.Add(operationPlanStep);

        if (policyStepId != 0)
        {
            steps = steps.Add(new PolicyPlanStep
            {
                Id = policyStepId,
                Targets = targets
            });
        }

        var costState = AddOperationStepCostState(current, stepId, stepDepth);
        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlog.Cost);

        var next = new PlanNode
        {
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            RemainingCost = remainingCost,
            Steps = steps,
            LastRequirementId = current.LastRequirementId,
            RequirementAliases = current.RequirementAliases,
            PolicySlots = policySlots,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths,
            EventStreamDirective = current.EventStreamDirective
        };

        possiblePlans.EnqueueBranches(next);
    }

    private bool CanSeedSourceSchemaNodeBranch(
        string schemaName,
        ImmutableStack<ConditionedFieldSelection> fieldsWithRequirements,
        ImmutableStack<ConditionedSelectionSet> unresolvable,
        SourceSchemaNodePlanningPolicy policy,
        ISelectionSetIndex index)
    {
        if (!fieldsWithRequirements.IsEmpty)
        {
            foreach (var entry in fieldsWithRequirements)
            {
                var selection = entry.FieldSelection;
                if (!selection.Field.Sources.TryGetMember(schemaName, out var sourceField)
                    || sourceField.Requirements is not { } sourceRequirements)
                {
                    return false;
                }

                var requirementIndex = index.ToBuilder();
                requirementIndex.Register(
                    selection.SelectionSetId,
                    sourceRequirements.Requirements);
                RegisterRequirementSelectionSets(
                    sourceRequirements.Requirements,
                    requirementIndex);

                var input = new SelectionSetPartitionerInput
                {
                    SchemaName = schemaName,
                    SelectionSet = new SelectionSet(
                        selection.SelectionSetId,
                        sourceRequirements.Requirements,
                        selection.Field.DeclaringType,
                        selection.Path),
                    SelectionSetIndex = requirementIndex,
                    TreatSourceExternalAsUnresolvable = true
                };

                var (resolvable, requirementUnresolvable, nestedRequirements, _, _) =
                    _partitioner.Partition(input);

                if (resolvable is not { Selections.Count: > 0 }
                    || !requirementUnresolvable.IsEmpty
                    || !nestedRequirements.IsEmpty)
                {
                    return false;
                }
            }

            return true;
        }

        return policy.CandidateGroupId is null && !unresolvable.IsEmpty;
    }

    private void PlanNode(
        NodeFieldWorkItem workItem,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog,
        PolicyPlanningSession planningSession)
    {
        var stepId = current.Steps.NextId();
        var fallbackQueryStepId = stepId + 1;
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;
        var nodeField = workItem.NodeField.Field;
        var responseName = nodeField.Alias?.Value ?? nodeField.Name.Value;
        var selectionPath = SelectionPath.Root.AppendField(responseName);
        var sourceSchemaResolution = _schema.NodeResolution == NodeResolution.SourceSchema;

        var idArgumentValue = nodeField.Arguments.First(a => a.Name.Value == "id").Value;

        var selectionSet = new SelectionSet(
            index.GetId(nodeField.SelectionSet!),
            nodeField.SelectionSet!,
            _schema.Types["Node"],
            selectionPath);

        var input = new SelectionSetByTypePartitionerInput { SelectionSet = selectionSet, SelectionSetIndex = index };

        (var sharedSelectionSet, var selectionSetsByType, index) = _selectionSetByTypePartitioner.Partition(input);

        var hasSharedClientSelections = sharedSelectionSet?.Selections.Any(
            selection => !IsTypeNameSelection(selection)) == true;
        ImmutableDictionary<string, ImmutableHashSet<string>>? candidateSchemasByType = null;
        var branches = selectionSetsByType;

        if (sourceSchemaResolution)
        {
            var abstractType = (FusionComplexTypeDefinition)selectionSet.Type;
            candidateSchemasByType = _sourceSchemaNodeCandidateResolver.Resolve(
                nodeField,
                abstractType);

            if (hasSharedClientSelections)
            {
                var branchesByType = selectionSetsByType.ToDictionary(
                    branch => branch.Type.Name,
                    StringComparer.Ordinal);
                var branchBuilder = ImmutableArray.CreateBuilder<SelectionSetByType>();
                var branchIndexBuilder = index.ToBuilder();

                foreach (var possibleType in _schema.GetPossibleTypes(
                    abstractType,
                    includeInaccessible: true).OrderBy(type => type.Name, StringComparer.Ordinal))
                {
                    if (branchesByType.TryGetValue(possibleType.Name, out var branch))
                    {
                        branchBuilder.Add(branch);
                    }
                    else
                    {
                        var sharedOnlyBranch = new SelectionSetNode(
                            sharedSelectionSet!.Selections);
                        branchIndexBuilder.RegisterConcreteBranch(
                            selectionSet.Id,
                            possibleType.Name,
                            sharedOnlyBranch);

                        branchBuilder.Add(
                            new SelectionSetByType(
                                possibleType,
                                sharedOnlyBranch));
                    }
                }

                branches = branchBuilder.ToImmutable();
                index = branchIndexBuilder;
            }
        }

        var sharedSelections = sharedSelectionSet?.Selections ?? [];
        if (sourceSchemaResolution)
        {
            sharedSelections = [new FieldNode(IntrospectionFieldNames.TypeName)];
        }
        else
        {
            if (sharedSelections.Count < 1 || !sharedSelections.Any(IsTypeNameSelection))
            {
                sharedSelections = [new FieldNode(IntrospectionFieldNames.TypeName), .. sharedSelections];
            }
        }

        var nodeConditions = ExtractConditions(workItem.NodeField);
        var nodeFieldSelectionSet = new SelectionSetNode(sharedSelections);
        var nodeFieldWithSelectionSet = nodeField.WithSelectionSet(nodeFieldSelectionSet);
        var fallbackQuerySelectionSet = new SelectionSetNode([nodeFieldWithSelectionSet]);

        var fallbackIndexBuilder = index.ToBuilder();
        fallbackIndexBuilder.Register(fallbackQuerySelectionSet);
        var fallbackPolicySelectionSet = new SelectionSet(
            fallbackIndexBuilder.GetId(fallbackQuerySelectionSet),
            fallbackQuerySelectionSet,
            _schema.QueryType,
            SelectionPath.Root);
        var fallbackTargets = CollectSpecializedPolicyTargets(
            fallbackPolicySelectionSet,
            nodeConditions);
        var (policyTargets, policyGates, policySlots) = BuildPolicyTargets(
            fallbackTargets,
            fallbackPolicySelectionSet,
            nodeConditions,
            includeSelectionSetObjectPolicy: true,
            selectionSetGateEligible: true,
            current.PolicySlots,
            planningSession);
        (fallbackQuerySelectionSet, _, _, _, var slotConditions) = ApplyPolicyGates(
            fallbackPolicySelectionSet,
            fallbackQuerySelectionSet,
            [],
            [],
            fallbackIndexBuilder,
            policyGates);

        var fallbackQueryBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetName(current.CreateOperationName(fallbackQueryStepId))
                .SetSelectionSet(fallbackQuerySelectionSet);

        (var fallbackQuery, index, _) = fallbackQueryBuilder.Build(fallbackIndexBuilder);

        var indexBuilder = index.ToBuilder();
        indexBuilder.Register(nodeFieldSelectionSet);
        indexBuilder.Register(fallbackQuerySelectionSet);
        index = indexBuilder;

        var fallbackQueryStep = new OperationPlanStep
        {
            Id = fallbackQueryStepId,
            Definition = fallbackQuery,
            Type = _schema.QueryType,
            SchemaName = null,
            RootSelectionSetId = index.GetId(fallbackQuery.SelectionSet),
            SelectionSets = SelectionSetIndexer.CreateIdSet(fallbackQuery.SelectionSet, index),
            Dependents = policyTargets.IsDefaultOrEmpty
                ? []
                : [fallbackQueryStepId + 1],
#if NET10_0_OR_GREATER
            Requirements = [],
#else
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
#endif
            Conditions = slotConditions,
            Target = SelectionPath.Root,
            Source = SelectionPath.Root
        };

        var nodeStep = new NodeFieldPlanStep
        {
            Id = stepId,
            ResponseName = responseName,
            IdValue = idArgumentValue,
            Conditions = slotConditions.Length == 0
                ? nodeConditions
                : [.. nodeConditions, .. slotConditions],
            FallbackQuery = fallbackQueryStep,
            SourceSchemaResolution = sourceSchemaResolution
        };

        foreach (var (type, selectionSetNode) in branches)
        {
            SourceSchemaNodePlanningPolicy? sourceSchemaNodePolicy = null;
            if (candidateSchemasByType is not null)
            {
                if (!candidateSchemasByType.TryGetValue(type.Name, out var candidateSchemas))
                {
                    continue;
                }

                sourceSchemaNodePolicy = new SourceSchemaNodePlanningPolicy(
                    candidateSchemas,
                    hasSharedClientSelections ? stepId : null);
            }

            var nodeSelectionSet = new SelectionSet(
                index.GetId(selectionSetNode),
                selectionSetNode,
                type,
                selectionPath.AppendFragment(type.Name));

            var newWorkItem = new NodeLookupWorkItem(
                Lookup: null,
                responseName,
                idArgumentValue,
                nodeSelectionSet,
                nodeConditions)
            {
                ParentDepth = stepDepth,
                SourceSchemaNodePolicy = sourceSchemaNodePolicy
            };

            backlog = backlog.Push(newWorkItem);
        }

        var costState = AddOperationStepCostState(current, fallbackQueryStepId, stepDepth);
        var remainingCost =
            PlannerCostEstimator.EstimateRemainingCost(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlog.Cost);

        var steps = current.Steps
            .Add(nodeStep)
            .Add(fallbackQueryStep);
        if (!policyTargets.IsDefaultOrEmpty)
        {
            steps = steps.Add(new PolicyPlanStep
            {
                Id = fallbackQueryStepId + 1,
                Targets = policyTargets
            });
        }

        var next = new PlanNode
        {
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            RemainingCost = remainingCost,
            Steps = steps,
            LastRequirementId = current.LastRequirementId,
            RequirementAliases = current.RequirementAliases,
            PolicySlots = policySlots,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths,
            EventStreamDirective = current.EventStreamDirective
        };

        possiblePlans.EnqueueBranches(next);
    }

    private static ExecutionNodeCondition[] ExtractConditions(NodeField nodeField)
    {
        var conditions = new List<ExecutionNodeCondition>();

        if (nodeField.ParentFragments is not null)
        {
            foreach (var fragment in nodeField.ParentFragments)
            {
                var fragmentConditions = ExtractConditions(fragment.Directives);

                if (fragmentConditions is not null)
                {
                    conditions.AddRange(fragmentConditions);
                }
            }
        }

        var nodeFieldConditions = ExtractConditions(nodeField.Field.Directives);

        if (nodeFieldConditions is not null)
        {
            conditions.AddRange(nodeFieldConditions);
        }

        return conditions.ToArray();
    }

    private static List<ExecutionNodeCondition>? ExtractConditions(IReadOnlyList<DirectiveNode> directives)
    {
        List<ExecutionNodeCondition>? conditions = null;

        foreach (var directive in directives)
        {
            var passingValue = directive.Name.Value switch
            {
                "skip" => false,
                "include" => true,
                _ => (bool?)null
            };

            if (passingValue.HasValue)
            {
                var ifArgument = directive.Arguments[0];
                var condition = new ExecutionNodeCondition
                {
                    VariableName = ((VariableNode)ifArgument.Value).Name.Value,
                    PassingValue = passingValue.Value,
                    Directive = directive
                };

                conditions ??= [];
                conditions.Add(condition);
            }
        }

        return conditions;
    }

    private static List<ArgumentNode> GetLookupArguments(Lookup lookup, string requirementKey)
    {
        var arguments = new List<ArgumentNode>();

        foreach (var argument in lookup.Arguments)
        {
            arguments.Add(
                new ArgumentNode(
                    new NameNode(argument.Name),
                    new VariableNode(new NameNode($"{requirementKey}_{argument.Name}"))));
        }

        return arguments;
    }

    private SelectionSetNode? ExtractResolvableChildSelections(
        int stepId,
        int stepDepth,
        FieldSelection selection,
        PlanNode current,
        SelectionSetIndexBuilder index,
        ref Backlog backlog,
        SourceSchemaNodePlanningPolicy? sourceSchemaNodePolicy,
        ImmutableArray<PolicyGate> policyGates)
    {
        if (selection.Node.SelectionSet is null)
        {
            return null;
        }

        var selectionSetId = index.GetId(selection.Node.SelectionSet);
        var selectionSetType = selection.Field.Type.AsTypeDefinition();

        // the field's path points at the selection set the field lives in. Its child
        // selections live one level deeper, so we append the field's response name to
        // root the partitioned children (and any re-entrant lookups derived from them)
        // at the correct path.
        var childPath = selection.Path.AppendField(
            selection.Node.Alias?.Value ?? selection.Node.Name.Value);
        var selectionSet = new SelectionSet(
            selectionSetId,
            selection.Node.SelectionSet,
            selectionSetType,
            childPath);

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = selectionSet,
            SelectionSetIndex = index,
            TreatSourceExternalAsUnresolvable = sourceSchemaNodePolicy is not null
        };

        var (resolvable, unresolvable, fieldsWithRequirements, _, _) = _partitioner.Partition(input);
        var hasResolvable = resolvable is not null;
        (resolvable, unresolvable, fieldsWithRequirements, _, _) = ApplyPolicyGates(
            selectionSet,
            resolvable ?? new SelectionSetNode([]),
            unresolvable,
            fieldsWithRequirements,
            index,
            policyGates);
        var descendantPolicy = sourceSchemaNodePolicy is null
            ? null
            : SourceSchemaNodePlanningPolicy.Descendant;
        backlog = backlog.PushUnresolvable(
            unresolvable,
            current.SchemaName,
            stepDepth,
            sourceSchemaNodePolicy: descendantPolicy);
        backlog = backlog.PushRequirements(
            fieldsWithRequirements,
            new StepConsumer(stepId),
            stepDepth,
            descendantPolicy);
        return hasResolvable ? resolvable : null;
    }

    // A leftover requirement selection set could not be inlined into any existing plan step.
    // Direct fields of the requiring entity are pushed as a lookup rooted at the entity itself.
    // A nested object or list field is instead re-rooted at the nested entity type (its own path
    // and key), so a requirement whose selection map descends through an intermediate entity owned
    // by one source schema into a leaf owned by another can be resolved by the nested entity's key
    // lookup, mirroring how a requiring field's own child selections are re-planned as re-entrant
    // lookups.
    private Backlog PushRequirementLeftover(
        Backlog backlog,
        SelectionSetIndexBuilder index,
        SelectionSetNode leftover,
        uint selectionSetId,
        ITypeDefinition entityType,
        SelectionPath path,
        string fromSchema,
        ImmutableHashSet<int> dependents,
        int parentDepth,
        ExecutionNodeCondition[] conditions,
        SourceSchemaNodePlanningPolicy? sourceSchemaNodePolicy)
    {
        var complexType = entityType as FusionComplexTypeDefinition;
        List<ISelectionNode>? directSelections = null;
        var descendantPolicy = sourceSchemaNodePolicy is null
            ? null
            : SourceSchemaNodePlanningPolicy.Descendant;

        foreach (var selection in leftover.Selections)
        {
            if (complexType is not null
                && selection is FieldNode { SelectionSet: { } childSelectionSet } fieldNode
                && complexType.Fields.TryGetField(
                    fieldNode.Name.Value,
                    allowInaccessibleFields: true,
                    out var field)
                && field.Type.AsTypeDefinition() is FusionComplexTypeDefinition childType
                && !_schema.GetPossibleLookups(childType).IsDefaultOrEmpty
                // only re-root when the intermediate field itself cannot be resolved by
                // re-entering a schema other than the one we came from. If another schema
                // owns it, the parent-rooted lookup already resolves it without crossing
                // an entity boundary.
                && field.Sources.Schemas.All(
                    schemaName => schemaName.Equals(fromSchema, StringComparison.Ordinal)))
            {
                var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

                if (!index.IsRegistered(childSelectionSet))
                {
                    index.Register(childSelectionSet);
                }

                backlog = backlog.Push(
                    new OperationWorkItem(
                        OperationWorkItemKind.Lookup,
                        new SelectionSet(
                            index.GetId(childSelectionSet),
                            childSelectionSet,
                            childType,
                            path.AppendField(responseName)),
                        FromSchema: fromSchema)
                    {
                        Dependents = dependents,
                        ParentDepth = parentDepth,
                        Conditions = conditions,
                        SourceSchemaNodePolicy = descendantPolicy
                    });
                continue;
            }

            directSelections ??= [];
            directSelections.Add(selection);
        }

        if (directSelections is not null)
        {
            var directSelectionSet = directSelections.Count == leftover.Selections.Count
                ? leftover
                : new SelectionSetNode(directSelections);
            index.Register(selectionSetId, directSelectionSet);

            backlog = backlog.Push(
                new OperationWorkItem(
                    OperationWorkItemKind.Lookup,
                    new SelectionSet(selectionSetId, directSelectionSet, entityType, path),
                    FromSchema: fromSchema)
                {
                    Dependents = dependents,
                    ParentDepth = parentDepth,
                    Conditions = conditions,
                    SourceSchemaNodePolicy = descendantPolicy
                });
        }

        return backlog;
    }

    private SelectionSetNode? TryInlineFieldRequirements(
        FieldRequirementWorkItem workItem,
        int dependentStepId,
        ref PlanNode current,
        OperationPlanStep currentStep,
        SelectionSetIndexBuilder index,
        ref Backlog backlog,
        ref ImmutableList<PlanStep> steps,
        out RequirementAliasContext requirementAliases)
    {
        var compositeField = workItem.Selection.Field;
        var sourceField = compositeField.Sources[current.SchemaName];
        var sourceRequirements = sourceField.Requirements!;
        var requirements = sourceRequirements.Requirements;
        requirementAliases = new RequirementAliasContext(sourceRequirements.Fields, current.RequirementAliases);

        if (index.IsRegistered(requirements))
        {
            // if we already used the same requirements object and have indexed it
            // we will do a deep clone of the syntax tree so that the planner does
            // not get confused and sees them as different requirements.
            requirements = SyntaxRewriter.Create(
                node =>
                {
                    if (node is FieldNode { SelectionSet: null } leafField)
                    {
                        return new FieldNode(
                            leafField.Name,
                            leafField.Alias,
                            leafField.Directives,
                            leafField.Arguments,
                            leafField.SelectionSet);
                    }

                    return node;
                }).Rewrite(requirements)!;
        }

        // A requirement field that shares a response name with a sibling client
        // selection but carries different arguments must resolve into its own response
        // slot; otherwise the requirement fetch and the client selection merge under the
        // same field and produce mismatched results (for example a limited list fetched
        // twice with different limits). The requirement may be resolved by a separate
        // re-entrant lookup that never inlines next to the client selection, so the
        // in-step collision check cannot see the conflict. Detect it here against the
        // sibling selections and mint an alias so the requirement carries a distinct
        // response name through partitioning, leftover re-rooting and the internal
        // operation.
        if (index.TryGetSelectionSet(workItem.Selection.SelectionSetId, out var siblingSelections))
        {
            var mintedAlias = false;

            foreach (var selection in requirements.Selections)
            {
                if (selection is FieldNode field
                    && HasArgumentConflict(field, siblingSelections.Selections))
                {
                    requirementAliases.MintAlias(field);
                    mintedAlias = true;
                }
            }

            if (mintedAlias)
            {
                requirements = requirementAliases.ApplyAliases(requirements);
            }
        }

        var fullRequirements = requirements;

        index.Register(
            workItem.Selection.SelectionSetId,
            requirements);

        // Register the requirement's nested selection sets so the partitioner can
        // resolve their identifiers while inlining into candidate steps. The aliased
        // requirement is inlined into the internal operation only after the loop
        // (aliases are minted during inlining), so the identifier registration that
        // inlining performs must happen up front here.
        RegisterRequirementSelectionSets(requirements, index);

        foreach (var (step, stepIndex, _) in current.GetCandidateSteps(workItem.Selection.SelectionSetId))
        {
            if (currentStep.Id == step.Id)
            {
                // we cannot inline the field requirements
                // into the step that requires.
                continue;
            }

            if (step.DependsOn(currentStep, steps))
            {
                // we cannot inline the field requirements into
                // an operation step that depends on the current step.
                continue;
            }

            if (!TryInlineSelectionSetIntoStep(
                step,
                workItem.Selection.SelectionSetId,
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.Path,
                requirements,
                dependentStepId,
                index,
                requirementAliases,
                out var updatedStep,
                out var unresolvable,
                treatSourceExternalAsUnresolvable: workItem.SourceSchemaNodePolicy is not null))
            {
                // if we cannot resolve any selection with the current source we cannot inline the
                // field requirements into this step.
                continue;
            }

            steps = steps.SetItem(stepIndex, updatedStep);
            requirements = null;

            if (!unresolvable.IsEmpty)
            {
                // if we have unresolvable parts of the requirements we will take the top level
                // parts that are not resolvable and try to resolve them in the next iteration.
                // Unresolvable child selections are pushed to the backlog and will be processed
                // in a later planing iteration.
                var top = unresolvable.Peek();
                if (top.SelectionSet.Id == workItem.Selection.SelectionSetId)
                {
                    unresolvable = unresolvable.Pop(out top);
                    requirements = top.SelectionSet.Node;
                }

                foreach (var entry in unresolvable.Reverse())
                {
                    backlog = backlog.Push(
                        new OperationWorkItem(
                            OperationWorkItemKind.Lookup,
                            entry.SelectionSet,
                            FromSchema: current.SchemaName)
                        {
                            ParentDepth = GetOperationStepDepth(current, step.Id),
                            Conditions = entry.Conditions,
                            SourceSchemaNodePolicy = workItem.SourceSchemaNodePolicy is null
                                ? null
                                : SourceSchemaNodePlanningPolicy.Descendant
                        });
                }
            }

            if (requirements is null)
            {
                // if requirements has become null we have inlined
                // all requirements and can stop.
                break;
            }
        }

        var internalRequirements = requirementAliases.ApplyAliases(fullRequirements);
        index.Register(workItem.Selection.SelectionSetId, internalRequirements);

        var internalOperation =
            InlineSelectionsIntoOverallOperation(
                current.InternalOperationDefinition,
                index,
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.SelectionSetId,
                internalRequirements);
        current = current with { InternalOperationDefinition = internalOperation };

        // Fallback: if no candidate step was found via exact selection set ID match,
        // walk the internal operation AST to find the nearest ancestor step and the
        // complete connector chain to the target selection set.
        if (requirements is not null
            && TryFindAncestorStepForRequirement(
                current.InternalOperationDefinition,
                steps,
                index,
                workItem.Selection.SelectionSetId,
                workItem.Selection.Path) is { } ancestorMatch
            && currentStep.Id != ancestorMatch.Step.Id
            && !ancestorMatch.Step.DependsOn(currentStep, steps))
        {
            if (TryInlineIntoAncestorStep(
                ancestorMatch, requirements, workItem.Selection.Path,
                dependentStepId, index, ref steps, out var unresolvable,
                treatSourceExternalAsUnresolvable: workItem.SourceSchemaNodePolicy is not null))
            {
                requirements = null;

                if (!unresolvable.IsEmpty)
                {
                    var top = unresolvable.Peek();
                    if (top.SelectionSet.Id == workItem.Selection.SelectionSetId)
                    {
                        unresolvable = unresolvable.Pop(out top);
                        requirements = top.SelectionSet.Node;
                    }

                    foreach (var entry in unresolvable.Reverse())
                    {
                        backlog = backlog.Push(
                            new OperationWorkItem(
                                OperationWorkItemKind.Lookup,
                                entry.SelectionSet,
                                FromSchema: current.SchemaName)
                            {
                                ParentDepth = GetOperationStepDepth(current, ancestorMatch.Step.Id),
                                Conditions = entry.Conditions,
                                SourceSchemaNodePolicy = workItem.SourceSchemaNodePolicy is null
                                    ? null
                                    : SourceSchemaNodePlanningPolicy.Descendant
                            });
                    }
                }
            }
        }

        return requirements;
    }

    private bool TryInlineSelectionSetIntoStep(
        OperationPlanStep step,
        uint targetSelectionSetId,
        ITypeDefinition selectionSetType,
        SelectionPath path,
        SelectionSetNode requirementSelections,
        int dependentStepId,
        SelectionSetIndexBuilder index,
        RequirementAliasContext requirementAliases,
        out OperationPlanStep updatedStep,
        out ImmutableStack<ConditionedSelectionSet> unresolvable,
        bool treatSourceExternalAsUnresolvable = false)
    {
        index.Register(targetSelectionSetId, requirementSelections);

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = step.SchemaName!,
            SelectionSet = new SelectionSet(
                targetSelectionSetId,
                requirementSelections,
                selectionSetType,
                path),
            SelectionSetIndex = index,
            TreatSourceExternalAsUnresolvable = treatSourceExternalAsUnresolvable
        };

        var (resolvable, partitionUnresolvable, _, _, _) = _partitioner.Partition(input);

        if (resolvable is not { Selections.Count: > 0 })
        {
            updatedStep = step;
            unresolvable = [];
            return false;
        }

        var existingSelectionSet = FindSelectionSet(
            step.Definition.SelectionSet,
            index,
            targetSelectionSetId);

        if (existingSelectionSet is not null)
        {
            List<ISelectionNode>? selectionsToInline = null;
            List<ISelectionNode>? collidingSelections = null;

            for (var i = 0; i < resolvable.Selections.Count; i++)
            {
                var selection = resolvable.Selections[i];

                if (selection is FieldNode field
                    && HasArgumentConflict(field, existingSelectionSet.Selections))
                {
                    var internalAlias = requirementAliases.MintAlias(field);
                    var aliasedField = CreateFieldWithAlias(field, internalAlias);

                    collidingSelections ??= [];
                    collidingSelections.Add(aliasedField);

                    if (selectionsToInline is null)
                    {
                        selectionsToInline = [with(resolvable.Selections.Count)];
                        for (var j = 0; j < i; j++)
                        {
                            selectionsToInline.Add(resolvable.Selections[j]);
                        }
                    }
                }
                else
                {
                    selectionsToInline?.Add(selection);
                }
            }

            if (collidingSelections is not null)
            {
                var conditions = Array.Empty<ExecutionNodeCondition>();

                if (!partitionUnresolvable.IsEmpty)
                {
                    var top = partitionUnresolvable.Peek();

                    if (top.SelectionSet.Id == targetSelectionSetId)
                    {
                        partitionUnresolvable = partitionUnresolvable.Pop(out top);
                        conditions = top.Conditions;

                        var mergedSelections = new List<ISelectionNode>(
                            top.SelectionSet.Node.Selections.Count + collidingSelections.Count);
                        mergedSelections.AddRange(top.SelectionSet.Node.Selections);
                        mergedSelections.AddRange(collidingSelections);
                        collidingSelections = mergedSelections;
                    }
                }

                var collidingSelectionSet = new SelectionSetNode(collidingSelections);
                index.Register(targetSelectionSetId, collidingSelectionSet);

                partitionUnresolvable = partitionUnresolvable.Push(
                    new ConditionedSelectionSet(
                        new SelectionSet(
                            targetSelectionSetId,
                            collidingSelectionSet,
                            selectionSetType,
                            path),
                        conditions));

                if (selectionsToInline is not { Count: > 0 })
                {
                    updatedStep = step;
                    unresolvable = partitionUnresolvable;
                    return true;
                }

                resolvable = resolvable.WithSelections(selectionsToInline);
            }
        }

        // the resolvable part of the requirement could be different from the requirement
        // if we are unable to inline the complete requirement into a single plan step.
        // in this case we will register the resolvable part as part of the requirements selection set
        // so that they logically belong together.
        if (resolvable != requirementSelections)
        {
            index.Register(targetSelectionSetId, resolvable);
        }

        var operation =
            InlineSelections(
                step.Definition,
                index,
                selectionSetType,
                targetSelectionSetId,
                resolvable);

        updatedStep = step with
        {
            Definition = operation,

            // the step containing the field requirements is now dependent on this step.
            Dependents = step.Dependents.Add(dependentStepId)
        };

        unresolvable = partitionUnresolvable;
        return true;

        static SelectionSetNode? FindSelectionSet(
            SelectionSetNode selectionSet,
            SelectionSetIndexBuilder index,
            uint targetSelectionSetId)
        {
            if (index.IsRegistered(selectionSet)
                && index.GetId(selectionSet) == targetSelectionSetId)
            {
                return selectionSet;
            }

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode { SelectionSet: not null } field:
                    {
                        var result = FindSelectionSet(
                            field.SelectionSet,
                            index,
                            targetSelectionSetId);

                        if (result is not null)
                        {
                            return result;
                        }

                        break;
                    }

                    case InlineFragmentNode inlineFragment:
                    {
                        var result = FindSelectionSet(
                            inlineFragment.SelectionSet,
                            index,
                            targetSelectionSetId);

                        if (result is not null)
                        {
                            return result;
                        }

                        break;
                    }
                }
            }

            return null;
        }
    }

    private static FieldNode CreateFieldWithAlias(FieldNode field, string internalAlias)
        => new(
            field.Name,
            new NameNode(internalAlias),
            field.Directives,
            field.Arguments,
            field.SelectionSet);

    private sealed class RequirementAliasContext(
        ImmutableArray<IValueSelectionNode?> fieldSelectionMaps,
        RequirementAliasRegistry registry)
    {
        private readonly Dictionary<IValueSelectionNode, string> _aliasesByMap = [];
        private readonly List<(RequirementFieldSelectionKey Key, string Alias)> _aliases = [];
        private RequirementAliasRegistry _registry = registry;

        public RequirementAliasRegistry Registry => _registry;

        public string? GetInternalAlias(IValueSelectionNode fieldSelectionMap)
            => _aliasesByMap.GetValueOrDefault(fieldSelectionMap);

        public string MintAlias(FieldNode field)
        {
            for (var i = 0; i < _aliases.Count; i++)
            {
                if (_aliases[i].Key.Matches(field))
                {
                    return _aliases[i].Alias;
                }
            }

            _registry = _registry.GetOrAdd(field, out var alias);
            var key = new RequirementFieldSelectionKey(field);
            _aliases.Add((key, alias));
            RecordMapAliases(key, alias);
            return alias;
        }

        public SelectionSetNode ApplyAliases(SelectionSetNode selectionSet)
        {
            if (_aliases.Count == 0)
            {
                return selectionSet;
            }

            List<ISelectionNode>? selections = null;

            for (var i = 0; i < selectionSet.Selections.Count; i++)
            {
                var selection = selectionSet.Selections[i];
                var rewritten = selection;

                if (selection is FieldNode field && TryGetAlias(field, out var alias))
                {
                    rewritten = field.Alias?.Value.Equals(alias, StringComparison.Ordinal) == true
                        ? field
                        : CreateFieldWithAlias(field, alias);
                }

                if (!ReferenceEquals(rewritten, selection) && selections is null)
                {
                    selections = [with(selectionSet.Selections.Count)];
                    for (var j = 0; j < i; j++)
                    {
                        selections.Add(selectionSet.Selections[j]);
                    }
                }

                selections?.Add(rewritten);
            }

            return selections is null ? selectionSet : selectionSet.WithSelections(selections);
        }

        private bool TryGetAlias(
            FieldNode field,
            [NotNullWhen(true)] out string? alias)
        {
            for (var i = 0; i < _aliases.Count; i++)
            {
                if (_aliases[i].Key.Matches(field))
                {
                    alias = _aliases[i].Alias;
                    return true;
                }
            }

            alias = null;
            return false;
        }

        private void RecordMapAliases(RequirementFieldSelectionKey key, string alias)
        {
            for (var i = 0; i < fieldSelectionMaps.Length; i++)
            {
                var map = fieldSelectionMaps[i];

                if (map is null
                    || _aliasesByMap.ContainsKey(map)
                    || !TryCreateRootFieldSelectionKey(map, out var mapKey)
                    || !key.Equals(mapKey))
                {
                    continue;
                }

                _aliasesByMap.Add(map, alias);
            }
        }

        private static bool TryCreateRootFieldSelectionKey(
            IValueSelectionNode selection,
            out RequirementFieldSelectionKey key)
        {
            switch (selection)
            {
                case PathNode path:
                    return TryCreateRootFieldSelectionKey(path.PathSegment, out key);

                case PathObjectValueSelectionNode pathObject:
                    return TryCreateRootFieldSelectionKey(pathObject.Path.PathSegment, out key);

                case PathListValueSelectionNode pathList:
                    return TryCreateRootFieldSelectionKey(pathList.Path.PathSegment, out key);

                case ObjectValueSelectionNode { Fields.Length: 1 } objectValue:
                    var field = objectValue.Fields[0];

                    if (field.ValueSelection is null)
                    {
                        key = new RequirementFieldSelectionKey(
                            field.Name.Value,
                            FieldSelectionMapValueNodeConverter.Convert(field.Arguments));
                        return true;
                    }

                    return TryCreateRootFieldSelectionKey(field.ValueSelection, out key);

                default:
                    key = default;
                    return false;
            }
        }

        private static bool TryCreateRootFieldSelectionKey(
            PathSegmentNode pathSegment,
            out RequirementFieldSelectionKey key)
        {
            key = new RequirementFieldSelectionKey(
                pathSegment.FieldName.Value,
                FieldSelectionMapValueNodeConverter.Convert(pathSegment.Arguments));
            return true;
        }
    }

    private readonly struct RequirementFieldSelectionKey
    {
        private readonly IReadOnlyList<ArgumentNode> _arguments;

        public RequirementFieldSelectionKey(FieldNode field)
            : this(field.Name.Value, field.Arguments)
        {
        }

        public RequirementFieldSelectionKey(string name, IReadOnlyList<ArgumentNode> arguments)
        {
            Name = name;
            _arguments = arguments;
        }

        public string Name { get; }

        public bool Matches(FieldNode field)
            => string.Equals(Name, field.Name.Value, StringComparison.Ordinal)
                && ArgumentsEqual(_arguments, field.Arguments);

        public bool Equals(RequirementFieldSelectionKey other)
            => string.Equals(Name, other.Name, StringComparison.Ordinal)
                && ArgumentsEqual(_arguments, other._arguments);

        private static bool ArgumentsEqual(
            IReadOnlyList<ArgumentNode> left,
            IReadOnlyList<ArgumentNode> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (var i = 0; i < left.Count; i++)
            {
                if (!SyntaxComparer.BySyntax.Equals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// An operation-level registry that assigns a stable internal alias to each distinct
    /// requirement-field identity (field name and arguments). Identical identities from any
    /// requiring field resolve to the same alias so their fetches merge, while identities that
    /// differ by arguments receive distinct aliases. The registry is threaded through the plan
    /// node like other operation-level plan state and is immutable so planner branches stay
    /// isolated.
    /// </summary>
    internal readonly struct RequirementAliasRegistry
    {
        private readonly ImmutableArray<(RequirementFieldSelectionKey Key, string Alias)> _entries;

        private RequirementAliasRegistry(
            ImmutableArray<(RequirementFieldSelectionKey Key, string Alias)> entries)
            => _entries = entries;

        public static RequirementAliasRegistry Empty { get; } = new([]);

        public RequirementAliasRegistry GetOrAdd(FieldNode field, out string alias)
        {
            var entries = _entries;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Key.Matches(field))
                {
                    alias = entries[i].Alias;
                    return this;
                }
            }

            alias = $"fusion__requirement_{field.Name.Value}_{entries.Length}";
            return new(entries.Add((new RequirementFieldSelectionKey(field), alias)));
        }
    }

    internal readonly record struct PolicyGateApplication(
        ImmutableArray<ImmutableArray<string>> Groups,
        PolicyDenialBehavior OnDenied);

    /// <summary>
    /// Defines the request and residual facets an application needs before capacity is applied.
    /// </summary>
    internal enum PolicySlotApplicationClass
    {
        SlotOnly,
        SlotAndResidual
    }

    [Flags]
    internal enum PolicySlotApplicationFacets
    {
        None = 0,
        SlotGate = 1,
        ResidualEvaluation = 2
    }

    /// <summary>
    /// Describes the immutable result of visiting one policy gate identity.
    /// </summary>
    internal readonly record struct PolicySlotAllocationResult(
        string Identity,
        bool IsAllocated,
        ImmutableArray<PolicySlotApplicationFacets> ApplicationFacets);

    /// <summary>
    /// Allocates policy gate identities in their first-encounter order.
    /// </summary>
    internal sealed class PolicySlotAllocationWalk
    {
        private readonly List<string> _allocated;
        private readonly List<string> _unallocated;

        public PolicySlotAllocationWalk(
            IEnumerable<string> allocated,
            IEnumerable<string> unallocated)
        {
            _allocated = [.. allocated];
            _unallocated = [.. unallocated];
        }

        public PolicySlotAllocationResult Visit(
            ImmutableArray<PolicyGateApplication> applications,
            PolicyDenialBehavior rmax,
            ImmutableArray<PolicySlotApplicationClass> applicationClasses)
        {
            if (applications.Length != applicationClasses.Length)
            {
                throw new ArgumentException(
                    "Each policy gate application must have an allocation classification.",
                    nameof(applicationClasses));
            }

            var identity = CreateIdentity(applications, rmax);
            var isAllocated = VisitIdentity(identity);
            var facets = ImmutableArray.CreateBuilder<PolicySlotApplicationFacets>(
                applicationClasses.Length);

            foreach (var applicationClass in applicationClasses)
            {
                facets.Add(applicationClass switch
                {
                    PolicySlotApplicationClass.SlotOnly => isAllocated
                        ? PolicySlotApplicationFacets.SlotGate
                        : PolicySlotApplicationFacets.ResidualEvaluation,
                    PolicySlotApplicationClass.SlotAndResidual => isAllocated
                        ? PolicySlotApplicationFacets.SlotGate | PolicySlotApplicationFacets.ResidualEvaluation
                        : PolicySlotApplicationFacets.ResidualEvaluation,
                    _ => throw new ArgumentOutOfRangeException(nameof(applicationClasses))
                });
            }

            return new PolicySlotAllocationResult(
                identity,
                isAllocated,
                facets.MoveToImmutable());
        }

        private bool VisitIdentity(string identity)
        {
            if (_allocated.Contains(identity, StringComparer.Ordinal))
            {
                return true;
            }

            if (_unallocated.Contains(identity, StringComparer.Ordinal))
            {
                return false;
            }

            if (_allocated.Count < PolicySlotRegistry.MaxPolicySlots)
            {
                _allocated.Add(identity);
                return true;
            }

            _unallocated.Add(identity);
            return false;
        }

        internal static string CreateIdentity(
            IEnumerable<PolicyGateApplication> applications,
            PolicyDenialBehavior rmax)
            => CreateKey(CanonicalizeApplications([.. applications]), rmax);

        private static string CreateKey(
            ImmutableArray<PolicyGateApplication> applications,
            PolicyDenialBehavior rmax)
        {
            var parts = new string[applications.Length + 1];
            parts[0] = ((int)rmax).ToString(System.Globalization.CultureInfo.InvariantCulture);

            for (var i = 0; i < applications.Length; i++)
            {
                var application = applications[i];
                var expressionKey = PolicyNameGroups.CreateCanonicalKey(application.Groups);
                parts[i + 1] = string.Concat(
                    ((int)application.OnDenied).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ":",
                    expressionKey.Length.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ":",
                    expressionKey);
            }

            return string.Join('|', parts);
        }

        internal static ImmutableArray<PolicyGateApplication> CanonicalizeApplications(
            ImmutableArray<PolicyGateApplication> applications)
            => [.. applications
                .GroupBy(application => (
                    Key: PolicyNameGroups.CreateCanonicalKey(application.Groups),
                    application.OnDenied))
                .Select(group => group.First())
                .OrderBy(
                    application => PolicyNameGroups.CreateCanonicalKey(application.Groups),
                    StringComparer.Ordinal)
                .ThenBy(application => application.OnDenied)];

        public ImmutableArray<string> AllocatedIdentities => [.. _allocated];

        public ImmutableArray<string> UnallocatedIdentities => [.. _unallocated];
    }

    /// <summary>
    /// Assigns stable ordinals to policy expressions and coordinate gates within one operation.
    /// </summary>
    internal readonly struct PolicySlotRegistry
    {
        /// <summary>
        /// Gets the maximum number of policy gate variables carried by one plan.
        /// </summary>
        public const int MaxPolicySlots = 64;

        private readonly ImmutableArray<PolicyConditionExpression> _expressions;
        private readonly ImmutableArray<string> _expressionKeys;
        private readonly ImmutableArray<PolicyConditionSlot> _entries;
        private readonly ImmutableArray<string> _gateKeys;
        private readonly ImmutableArray<string> _overflowedGateKeys;
        private readonly ImmutableArray<PolicyPlanEntry> _policies;

        private PolicySlotRegistry(
            ImmutableArray<PolicyConditionExpression> expressions,
            ImmutableArray<string> expressionKeys,
            ImmutableArray<PolicyConditionSlot> entries,
            ImmutableArray<string> gateKeys,
            ImmutableArray<string> overflowedGateKeys,
            ImmutableArray<PolicyPlanEntry> policies)
        {
            _expressions = expressions;
            _expressionKeys = expressionKeys;
            _entries = entries;
            _gateKeys = gateKeys;
            _overflowedGateKeys = overflowedGateKeys;
            _policies = policies;
        }

        public static PolicySlotRegistry Empty { get; } = new([], [], [], [], [], []);

        public ImmutableArray<PolicyConditionExpression> Expressions => _expressions;

        public ImmutableArray<PolicyConditionSlot> Entries => _entries;

        public ImmutableArray<PolicyPlanEntry> Policies => _policies;

        public int OverflowedIdentityCount => _overflowedGateKeys.Length;

        /// <summary>
        /// Adds every policy name and planning-time requirement fingerprint to the operation inventory.
        /// </summary>
        internal PolicySlotRegistry AddInventory(
            IEnumerable<PolicyApplication> applications,
            PolicyPlanningState policyState)
        {
            var policies = _policies;

            foreach (var application in applications)
            {
                foreach (var group in application.Groups)
                {
                    foreach (var name in group)
                    {
                        var hash = policyState.RequirementHashes.TryGetValue(name, out var knownHash)
                            ? knownHash
                            : PolicyPlanEntry.ComputeRequirementHash(null);

                        if (!policies.Any(entry =>
                            entry.PolicyName.Equals(name, StringComparison.Ordinal)
                            && entry.RequirementHash == hash))
                        {
                            policies = policies.Add(new PolicyPlanEntry
                            {
                                PolicyName = name,
                                RequirementHash = hash
                            });
                        }
                    }
                }
            }

            if (policies == _policies)
            {
                return this;
            }

            policies = [.. policies
                .OrderBy(entry => entry.PolicyName, StringComparer.Ordinal)
                .ThenBy(entry => entry.RequirementHash)];

            return new PolicySlotRegistry(
                _expressions,
                _expressionKeys,
                _entries,
                _gateKeys,
                _overflowedGateKeys,
                policies);
        }

        /// <summary>
        /// Gets or creates the gate for a canonical application tuple and residual threshold.
        /// </summary>
        internal bool TryGetOrAdd(
            ImmutableArray<PolicyGateApplication> applications,
            ImmutableArray<PolicySlotApplicationClass> applicationClasses,
            PolicyDenialBehavior rmax,
            PolicyConditionCoordinate coordinate,
            out PolicyConditionSlot slot,
            out PolicySlotRegistry registry)
        {
            var expressions = _expressions;
            var expressionKeys = _expressionKeys;
            var orderedReferences = ImmutableArray.CreateBuilder<PolicyConditionApplication>(
                applications.Length);

            foreach (var application in applications)
            {
                var expressionKey = PolicyNameGroups.CreateCanonicalKey(application.Groups);
                var expressionOrdinal = expressionKeys.IndexOf(expressionKey);

                if (expressionOrdinal < 0)
                {
                    expressionOrdinal = expressions.Length;
                    expressions = expressions.Add(new PolicyConditionExpression
                    {
                        Ordinal = expressionOrdinal,
                        Groups = application.Groups,
                        Text = PolicyNameGroups.Format(application.Groups)
                    });
                    expressionKeys = expressionKeys.Add(expressionKey);
                }

                orderedReferences.Add(new PolicyConditionApplication
                {
                    ExpressionOrdinal = expressionOrdinal,
                    OnDenied = application.OnDenied
                });
            }

            coordinate = coordinate with { Applications = orderedReferences.ToImmutable() };
            var canonicalApplications = PolicySlotAllocationWalk.CanonicalizeApplications(applications);
            var key = CreateIdentity(canonicalApplications, rmax);
            var allocationWalk = new PolicySlotAllocationWalk(_gateKeys, _overflowedGateKeys);
            var allocation = allocationWalk.Visit(applications, rmax, applicationClasses);

            if (!allocation.IsAllocated)
            {
                slot = null!;
                registry = new PolicySlotRegistry(
                    _expressions,
                    _expressionKeys,
                    _entries,
                    _gateKeys,
                    allocationWalk.UnallocatedIdentities,
                    _policies);
                return false;
            }

            for (var i = 0; i < _entries.Length; i++)
            {
                if (!_gateKeys[i].Equals(key, StringComparison.Ordinal))
                {
                    continue;
                }

                slot = AddCoordinate(AddGuardMasks(_entries[i], coordinate.LiveGuardMasks), coordinate);
                var entries = ReferenceEquals(slot, _entries[i])
                    ? _entries
                    : _entries.SetItem(i, slot);
                registry = entries == _entries
                    ? this
                    : new PolicySlotRegistry(
                        _expressions,
                        expressionKeys,
                        entries,
                        _gateKeys,
                        _overflowedGateKeys,
                        _policies);
                return true;
            }

            var references = ImmutableArray.CreateBuilder<PolicyConditionApplication>(
                canonicalApplications.Length);

            foreach (var application in canonicalApplications)
            {
                var expressionKey = PolicyNameGroups.CreateCanonicalKey(application.Groups);
                var expressionOrdinal = expressionKeys.IndexOf(expressionKey);

                references.Add(new PolicyConditionApplication
                {
                    ExpressionOrdinal = expressionOrdinal,
                    OnDenied = application.OnDenied
                });
            }

            var canonicalReferences = references
                .ToImmutable()
                .OrderBy(application => application.ExpressionOrdinal)
                .ThenBy(application => application.OnDenied)
                .ToImmutableArray();

            slot = new PolicyConditionSlot
            {
                Ordinal = _entries.Length,
                Applications = canonicalReferences,
                Rmax = rmax,
                GuardMasks = coordinate.LiveGuardMasks,
                Coordinates = [coordinate]
            };

            registry = new PolicySlotRegistry(
                expressions,
                expressionKeys,
                _entries.Add(slot),
                allocationWalk.AllocatedIdentities,
                _overflowedGateKeys,
                _policies);
            return true;
        }

        internal static string CreateIdentity(
            IEnumerable<PolicyGateApplication> applications,
            PolicyDenialBehavior rmax)
            => PolicySlotAllocationWalk.CreateIdentity(applications, rmax);

        private static PolicyConditionSlot AddGuardMasks(
            PolicyConditionSlot slot,
            ImmutableArray<ulong> guardMasks)
        {
            var canonical = CanonicalizeGuardMasks(slot.GuardMasks.AddRange(guardMasks));
            if (slot.GuardMasks.SequenceEqual(canonical))
            {
                return slot;
            }

            return slot with { GuardMasks = canonical };
        }

        private static PolicyConditionSlot AddCoordinate(
            PolicyConditionSlot slot,
            PolicyConditionCoordinate coordinate)
        {
            for (var i = 0; i < slot.Coordinates.Length; i++)
            {
                var current = slot.Coordinates[i];
                if (!current.TypeName.Equals(coordinate.TypeName, StringComparison.Ordinal)
                    || !string.Equals(current.FieldName, coordinate.FieldName, StringComparison.Ordinal)
                    || current.IsRoot != coordinate.IsRoot)
                {
                    continue;
                }

                var responseNames = current.ResponseNames;
                var responseNamesChanged = false;
                foreach (var responseName in coordinate.ResponseNames)
                {
                    if (!responseNames.Contains(responseName, StringComparer.Ordinal))
                    {
                        responseNames = responseNames.Add(responseName);
                        responseNamesChanged = true;
                    }
                }

                var liveGuardMasks = CanonicalizeGuardMasks(
                    current.LiveGuardMasks.AddRange(coordinate.LiveGuardMasks));
                var gateGuardMasks = CanonicalizeGuardMasks(
                    current.GateGuardMasks.AddRange(coordinate.GateGuardMasks));
                if (current.LiveGuardMasks.SequenceEqual(liveGuardMasks)
                    && current.GateGuardMasks.SequenceEqual(gateGuardMasks)
                    && !responseNamesChanged)
                {
                    return slot;
                }

                if (responseNamesChanged)
                {
                    responseNames = [.. responseNames.Order(StringComparer.Ordinal)];
                }

                var updated = current with
                {
                    LiveGuardMasks = liveGuardMasks,
                    GateGuardMasks = gateGuardMasks,
                    ResponseNames = responseNames,
                };
                return slot with { Coordinates = slot.Coordinates.SetItem(i, updated) };
            }

            return slot with { Coordinates = slot.Coordinates.Add(coordinate) };
        }

        private static ImmutableArray<ulong> CanonicalizeGuardMasks(
            ImmutableArray<ulong> guardMasks)
        {
            if (guardMasks.IsDefaultOrEmpty)
            {
                return [];
            }

            if (guardMasks.Contains(0))
            {
                return [0];
            }

            var ordered = guardMasks.Distinct().Order().ToArray();
            var canonical = ImmutableArray.CreateBuilder<ulong>(ordered.Length);
            foreach (var mask in ordered)
            {
                if (!canonical.Any(existing => (mask & existing) == existing))
                {
                    canonical.Add(mask);
                }
            }

            return canonical.ToImmutable();
        }
    }

    private static bool HasArgumentConflict(
        FieldNode requirementField,
        IReadOnlyList<ISelectionNode> existingSelections)
    {
        var responseName = requirementField.Alias?.Value ?? requirementField.Name.Value;

        foreach (var selection in existingSelections)
        {
            if (selection is not FieldNode existingField)
            {
                continue;
            }

            var existingResponseName = existingField.Alias?.Value ?? existingField.Name.Value;

            if (!responseName.Equals(existingResponseName, StringComparison.Ordinal))
            {
                continue;
            }

            if (requirementField.Arguments.Count != existingField.Arguments.Count)
            {
                return true;
            }

            for (var i = 0; i < requirementField.Arguments.Count; i++)
            {
                if (!SyntaxComparer.BySyntax.Equals(
                    requirementField.Arguments[i],
                    existingField.Arguments[i]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private OperationDefinitionNode InlineSelectionsIntoOverallOperation(
        OperationDefinitionNode operation,
        SelectionSetIndexBuilder index,
        ITypeDefinition selectionSetType,
        uint targetSelectionSetId,
        SelectionSetNode selectionsToInline)
    {
        // If we're looking for a selection set in the overall operation,
        // we need to ensure that we're not looking for a cloned selection set
        // and instead are looking for the original selection set.
        // Cloned selections can happen for instance if we're expanding an interface
        // selection set to multiple selection sets for each concrete type.
        if (index.TryGetOriginalIdFromCloned(targetSelectionSetId, out var originalId))
        {
            targetSelectionSetId = originalId;
        }

        if (index.TryTakeConcreteBranchScope(
            targetSelectionSetId,
            out var branchScope))
        {
            index.Register(targetSelectionSetId, selectionsToInline);

            selectionsToInline = new SelectionSetNode(
            [
                new InlineFragmentNode(
                    null,
                    new NamedTypeNode(branchScope.TypeCondition),
                    [],
                    selectionsToInline)
            ]);

            targetSelectionSetId = branchScope.ParentSelectionSetId;
        }

        return InlineSelections(
            operation,
            index,
            selectionSetType,
            targetSelectionSetId,
            selectionsToInline,
            inlineInternal: true);
    }

    private static void RegisterRequirementSelectionSets(
        SelectionSetNode selectionSet,
        SelectionSetIndexBuilder index)
    {
        var backlog = new Stack<SelectionSetNode>();
        backlog.Push(selectionSet);

        while (backlog.TryPop(out var current))
        {
            if (!index.IsRegistered(current))
            {
                index.Register(current);
            }

            foreach (var selection in current.Selections)
            {
                switch (selection)
                {
                    case FieldNode { SelectionSet: { } fieldSelectionSet }:
                        backlog.Push(fieldSelectionSet);
                        break;

                    case InlineFragmentNode { SelectionSet: { } fragmentSelectionSet }:
                        backlog.Push(fragmentSelectionSet);
                        break;
                }
            }
        }
    }

    private OperationDefinitionNode InlineSelections(
        OperationDefinitionNode operation,
        SelectionSetIndexBuilder index,
        ITypeDefinition selectionSetType,
        uint targetSelectionSetId,
        SelectionSetNode selectionsToInline,
        bool inlineInternal = false)
    {
        List<SelectionSetNode>? backlog = null;
        var didInline = false;

        var rewriter = SyntaxRewriter.Create<List<ISyntaxNode>>(
            rewrite: (node, path) =>
            {
                if (node is not SelectionSetNode selectionSet)
                {
                    return node;
                }

                // if the node was rewritten we keep track that the rewritten node and
                // the original node are semantically equivalent.
                var originalSelectionSet = (SelectionSetNode)path.Peek();
                var id = index.GetId(originalSelectionSet);

                if (!ReferenceEquals(originalSelectionSet, selectionSet))
                {
                    index.Register(originalSelectionSet, selectionSet);
                }

                if (targetSelectionSetId != id)
                {
                    return node;
                }

                SelectionSetNode newSelectionSet;

                if (inlineInternal)
                {
                    var size = selectionSet.Selections.Count + selectionsToInline.Selections.Count;
                    var selections = new List<ISelectionNode>(size);
                    selections.AddRange(originalSelectionSet.Selections);

                    foreach (var selection in selectionsToInline.Selections)
                    {
                        var markedSelection = MarkInternalSubtree(selection, index);

                        selections.Add(markedSelection);

                        switch (markedSelection)
                        {
                            case FieldNode field:
                                IndexInternalSelections(field.SelectionSet, index, ref backlog);
                                break;

                            case InlineFragmentNode inlineFragment:
                                IndexInternalSelections(inlineFragment.SelectionSet, index, ref backlog);
                                break;
                        }
                    }

                    newSelectionSet = new SelectionSetNode(selections);
                }
                else
                {
                    newSelectionSet = _mergeRewriter.Merge(
                        selectionSet,
                        selectionsToInline,
                        selectionSetType,
                        index);
                }

                didInline = true;

                index.Register(originalSelectionSet, newSelectionSet);
                return newSelectionSet;
            },
            enter: (node, path) =>
            {
                path.Push(node);
                return path;
            },
            leave: (_, path) => path.Pop());

        var rewrittenOperation = (OperationDefinitionNode)rewriter.Rewrite(operation, [])!;

        if (!didInline)
        {
            throw new InvalidOperationException(
                $"Selections `{selectionsToInline}` could not be inlined into selection set of type "
                + $"'{selectionSetType.Name}', as no selection set with the id {targetSelectionSetId} was found.");
        }

        return rewrittenOperation;

        static ISelectionNode MarkInternalSubtree(
            ISelectionNode selection,
            SelectionSetIndexBuilder index)
        {
            switch (selection)
            {
                case FieldNode field:
                    var fieldWithDirectives = field.WithDirectives(AddInternalDirective(field));

                    if (field.SelectionSet is null)
                    {
                        return fieldWithDirectives;
                    }

                    return fieldWithDirectives.WithSelectionSet(
                        MarkInternalSelections(field.SelectionSet, index));

                case InlineFragmentNode { SelectionSet: { } selectionSet } inlineFragment:
                    return inlineFragment.WithSelectionSet(MarkInternalSelections(selectionSet, index));

                default:
                    return selection;
            }
        }

        static SelectionSetNode MarkInternalSelections(
            SelectionSetNode selectionSet,
            SelectionSetIndexBuilder index)
        {
            var selections = new List<ISelectionNode>(selectionSet.Selections.Count);

            foreach (var childSelection in selectionSet.Selections)
            {
                selections.Add(MarkInternalSubtree(childSelection, index));
            }

            var markedSelectionSet = selectionSet.WithSelections(selections);

            if (!index.IsRegistered(selectionSet))
            {
                index.Register(selectionSet);
            }

            index.Register(selectionSet, markedSelectionSet);

            return markedSelectionSet;
        }

        static IReadOnlyList<DirectiveNode> AddInternalDirective(IHasDirectives selection)
        {
            var directives = new List<DirectiveNode>(selection.Directives.Count + 1);

            if (selection.Directives.Count > 0)
            {
                directives.AddRange(selection.Directives);
            }

            if (!directives.Any(t => t.Name.Value.Equals("fusion__requirement", StringComparison.Ordinal)))
            {
                directives.Add(new DirectiveNode("fusion__requirement"));
            }

            return directives;
        }

        // when we inline selections into the internal operation definition
        // we inline them as separate non-mergeable. This is so we can better
        // keep track of what is data and what is requirement.
        //
        static void IndexInternalSelections(
            SelectionSetNode? selectionSet,
            SelectionSetIndexBuilder index,
            ref List<SelectionSetNode>? backlog)
        {
            if (selectionSet is null)
            {
                return;
            }

            backlog ??= [];
            backlog.Clear();
            backlog.Push(selectionSet);

            while (backlog.TryPop(out var current))
            {
                if (!index.IsRegistered(current))
                {
                    index.Register(current);
                }

                foreach (var selection in current.Selections)
                {
                    switch (selection)
                    {
                        case FieldNode { SelectionSet: { } fieldSelectionSet }:
                            backlog.Push(fieldSelectionSet);
                            break;

                        case InlineFragmentNode { SelectionSet: { } fragmentSelectionSet }:
                            backlog.Push(fragmentSelectionSet);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns whether the composite schema has any interface field whose ownership diverges
    /// across concrete types (see <see cref="InterfaceFieldOwnershipDiverges"/>). The result is
    /// computed once per schema and cached so that operations against a schema without divergence
    /// (the common case) skip the expansion walk entirely.
    /// </summary>
    private bool SchemaHasDivergentInterfaceFields()
    {
        if (_schemaHasDivergentInterfaceFields is { } cached)
        {
            return cached;
        }

        var result = false;

        foreach (var type in _schema.Types)
        {
            if (type is not FusionInterfaceTypeDefinition interfaceType)
            {
                continue;
            }

            foreach (var field in interfaceType.Fields)
            {
                if (InterfaceFieldOwnershipDiverges(interfaceType, field.Name))
                {
                    result = true;
                    break;
                }
            }

            if (result)
            {
                break;
            }
        }

        _schemaHasDivergentInterfaceFields = result;
        return result;
    }

    /// <summary>
    /// Expands interface-level fields whose ownership diverges across concrete types into a
    /// per-concrete inline fragment (one <c>... on ConcreteType { field }</c> per possible type),
    /// leaving every non-diverging selection untouched.
    /// </summary>
    /// <remarks>
    /// The rewrite is response equivalent to the un-expanded operation: each fragment reuses the
    /// original field node verbatim, so its response name (alias), argument values, and directives
    /// (for example <c>@skip</c>/<c>@include</c>) are preserved on every copy, and the runtime
    /// response shape is unchanged. It merely moves the field from the interface level to the
    /// concrete level so a source schema that no longer owns the field on a concrete type (for
    /// example after an <c>@override</c>) routes that branch to the owner instead of resolving a
    /// stale value at the interface level.
    /// </remarks>
    private OperationDefinitionNode ExpandDivergentInterfaceFields(
        OperationDefinitionNode operation,
        ITypeDefinition rootType)
    {
        var rewriter = SyntaxRewriter.Create<Stack<ITypeDefinition>>(
            (node, path) =>
            {
                if (node is not SelectionSetNode selectionSet
                    || path.Peek() is not FusionInterfaceTypeDefinition interfaceType)
                {
                    return node;
                }

                List<ISelectionNode>? rewritten = null;

                for (var i = 0; i < selectionSet.Selections.Count; i++)
                {
                    var selection = selectionSet.Selections[i];

                    // A field selected directly on an interface can look resolvable in a source
                    // schema because the interface declares it there, even though a concrete type
                    // that implements the interface there no longer owns the field (for example
                    // after an @override moved it to another schema). Expanding the field into a
                    // per-concrete inline fragment lets the planner route each branch to its owner
                    // instead of resolving a stale value at the interface level.
                    if (selection is FieldNode fieldNode
                        && !fieldNode.Name.Value.Equals(IntrospectionFieldNames.TypeName)
                        && InterfaceFieldOwnershipDiverges(interfaceType, fieldNode.Name.Value))
                    {
                        rewritten ??= [.. selectionSet.Selections.Take(i)];

                        foreach (var possibleType in _schema.GetPossibleTypes(interfaceType, includeInaccessible: true))
                        {
                            rewritten.Add(new InlineFragmentNode(
                                null,
                                new NamedTypeNode(possibleType.Name),
                                [],
                                new SelectionSetNode([fieldNode])));
                        }
                    }
                    else
                    {
                        rewritten?.Add(selection);
                    }
                }

                return rewritten is null
                    ? node
                    : selectionSet.WithSelections(rewritten);
            },
            (node, path) =>
            {
                if (node is FieldNode { SelectionSet: not null } fieldNode
                    && path.Peek() is FusionComplexTypeDefinition complexType)
                {
                    var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                    path.Push(field.Type.NamedType());
                }
                else if (node is InlineFragmentNode { TypeCondition: { } typeCondition })
                {
                    path.Push(_schema.Types.GetType(typeCondition.Name.Value, allowInaccessibleFields: true));
                }

                return path;
            },
            (node, path) =>
            {
                if (node is FieldNode { SelectionSet: not null } or InlineFragmentNode { TypeCondition: not null })
                {
                    path.Pop();
                }
            });

        var context = new Stack<ITypeDefinition>();
        context.Push(rootType);

        return (OperationDefinitionNode)rewriter.Rewrite(operation, context)!;
    }

    /// <summary>
    /// Determines whether a field declared on an interface has diverging ownership across the
    /// interface's possible object types. Divergence occurs when a source schema declares the
    /// field on the interface, yet a concrete type that implements the interface in that schema
    /// does not own the field there (for example after an <c>@override</c> moved the field to
    /// another schema). Such a field cannot be resolved uniformly at the interface level.
    /// </summary>
    private bool InterfaceFieldOwnershipDiverges(
        FusionInterfaceTypeDefinition interfaceType,
        string fieldName)
    {
        if (!interfaceType.Fields.TryGetField(fieldName, allowInaccessibleFields: true, out var interfaceField))
        {
            return false;
        }

        foreach (var interfaceSource in interfaceField.Sources)
        {
            // Requirements keep their existing requirement-inlining handling, and an external
            // interface field is not claimed as resolvable by that schema in the first place.
            if (interfaceSource.IsExternal || interfaceSource.Requirements is not null)
            {
                continue;
            }

            var schemaName = interfaceSource.SchemaName;

            foreach (var possibleType in _schema.GetPossibleTypes(interfaceType, includeInaccessible: true))
            {
                // Only concrete types this schema can return through the interface are relevant.
                if (!possibleType.Sources.TryGetMember(schemaName, out var typeSource)
                    || !typeSource.Implements.Contains(interfaceType.Name))
                {
                    continue;
                }

                if (!possibleType.Fields.TryGetField(fieldName, allowInaccessibleFields: true, out var concreteField)
                    || !concreteField.Sources.TryGetMember(schemaName, out var concreteSource)
                    || concreteSource.IsExternal)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private OperationDefinitionNode AddTypeNameToAbstractSelections(
        OperationDefinitionNode operation,
        ITypeDefinition rootType)
    {
        var rewriter = SyntaxRewriter.Create<Stack<ITypeDefinition>>(
            (node, path) =>
            {
                if (node is not FieldNode { SelectionSet: { } selectionSet } fieldNode)
                {
                    return node;
                }

                var type = path.Peek();

                if (type.IsAbstractType() && !selectionSet.Selections.Any(IsTypeNameSelection))
                {
                    // we add the __typename field to all selection sets that have
                    // an abstract type context as we need the type context for
                    // runtime decisions.
                    //
                    // The __typename field is marked as a requirement to differentiate between a user
                    // required __typename and a runtime required type information.
                    var typenameNode = new FieldNode(IntrospectionFieldNames.TypeName)
                        .WithDirectives([new DirectiveNode("fusion__requirement")]);

                    return fieldNode.WithSelectionSet(new SelectionSetNode([
                        typenameNode, .. selectionSet.Selections
                    ]));
                }

                return node;
            },
            (node, path) =>
            {
                if (node is FieldNode { SelectionSet: not null } fieldNode
                    && path.Peek() is FusionComplexTypeDefinition complexType)
                {
                    var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                    path.Push(field.Type.NamedType());
                }
                else if (node is InlineFragmentNode { TypeCondition: { } typeCondition })
                {
                    path.Push(_schema.Types.GetType(typeCondition.Name.Value, allowInaccessibleFields: true));
                }

                return path;
            },
            (node, path) =>
            {
                if (node is FieldNode { SelectionSet: not null } or InlineFragmentNode { TypeCondition: not null })
                {
                    path.Pop();
                }
            });

        var context = new Stack<ITypeDefinition>();
        context.Push(rootType);

        return (OperationDefinitionNode)rewriter.Rewrite(operation, context)!;
    }

    private static bool IsTypeNameSelection(ISelectionNode selection)
    {
        if (selection is FieldNode field)
        {
            return field.Name.Value.Equals(IntrospectionFieldNames.TypeName)
                && field.Alias is null;
        }

        return false;
    }

    private AncestorStepMatch? TryFindAncestorStepForRequirement(
        OperationDefinitionNode internalOperation,
        ImmutableList<PlanStep> steps,
        SelectionSetIndexBuilder index,
        uint targetSelectionSetId,
        SelectionPath targetPath)
    {
        // Walk the internal operation AST depth-first, tracking:
        // - A stack of (SelectionSetNode, ISelectionNode, ITypeDefinition) from root to current
        // - The current type at each level
        var path = new List<(SelectionSetNode SelectionSet, ISelectionNode ConnectingNode, ITypeDefinition Type)>();
        var rootType = _schema.GetOperationType(internalOperation.Operation);

        return WalkSelectionSet(
            internalOperation.SelectionSet, rootType, path,
            SelectionPath.Root, _schema, steps, index, targetSelectionSetId, targetPath);

        static AncestorStepMatch? WalkSelectionSet(
            SelectionSetNode selectionSet,
            ITypeDefinition currentType,
            List<(SelectionSetNode SelectionSet, ISelectionNode ConnectingNode, ITypeDefinition Type)> path,
            SelectionPath currentPath,
            FusionSchemaDefinition schema,
            ImmutableList<PlanStep> steps,
            SelectionSetIndexBuilder index,
            uint targetSelectionSetId,
            SelectionPath targetPath)
        {
            var selectionSetId = index.GetId(selectionSet);

            if (selectionSetId == targetSelectionSetId
                && currentPath.Equals(targetPath))
            {
                // Found the target. Trace back to find the deepest ancestor SS ID
                // that exists in any step's SelectionSets.
                for (var i = path.Count - 1; i >= 0; i--)
                {
                    var entry = path[i];

                    if (entry.ConnectingNode is not InlineFragmentNode)
                    {
                        continue;
                    }

                    var candidateSelectionSetId = index.GetId(entry.SelectionSet);

                    for (var s = 0; s < steps.Count; s++)
                    {
                        if (steps[s] is OperationPlanStep step
                            && step.SelectionSets.Contains(candidateSelectionSetId)
                            && step.Target.IsParentOfOrSame(targetPath)
                            && !string.IsNullOrEmpty(step.SchemaName)
                            && IsConnectorChainResolvable(path, i, currentType, step.SchemaName))
                        {
                            var connectors = new List<AncestorConnector>(path.Count - i);
                            for (var j = i; j < path.Count; j++)
                            {
                                connectors.Add(
                                    new AncestorConnector(
                                        index.GetId(path[j].SelectionSet),
                                        path[j].ConnectingNode));
                            }

                            return new AncestorStepMatch(
                                step,
                                s,
                                candidateSelectionSetId,
                                entry.Type,
                                targetSelectionSetId,
                                currentType,
                                connectors);
                        }
                    }
                }

                return null;
            }

            foreach (var selection in selectionSet.Selections)
            {
                switch (selection)
                {
                    case FieldNode { SelectionSet: not null } fieldNode:
                        if (currentType is FusionComplexTypeDefinition complexType
                            && complexType.Fields.TryGetField(
                                fieldNode.Name.Value,
                                allowInaccessibleFields: true,
                                out var field))
                        {
                            var fieldType = field.Type.NamedType();
                            path.Add((selectionSet, fieldNode, currentType));
                            var result = WalkSelectionSet(
                                fieldNode.SelectionSet,
                                fieldType,
                                path,
                                currentPath.AppendField(fieldNode.Alias?.Value ?? fieldNode.Name.Value),
                                schema,
                                steps,
                                index,
                                targetSelectionSetId,
                                targetPath);
                            if (result is not null)
                            {
                                return result;
                            }
                            path.RemoveAt(path.Count - 1);
                        }
                        break;

                    case InlineFragmentNode inlineFragment:
                        var fragmentType = inlineFragment.TypeCondition is not null
                            ? schema.Types.GetType(
                                inlineFragment.TypeCondition.Name.Value,
                                allowInaccessibleFields: true)
                            : currentType;

                        path.Add((selectionSet, inlineFragment, currentType));
                        var fragmentPath = inlineFragment.TypeCondition is null
                            ? currentPath
                            : currentPath.AppendFragment(inlineFragment.TypeCondition.Name.Value);
                        var fragmentResult = WalkSelectionSet(
                            inlineFragment.SelectionSet,
                            fragmentType,
                            path,
                            fragmentPath,
                            schema,
                            steps,
                            index,
                            targetSelectionSetId,
                            targetPath);
                        if (fragmentResult is not null)
                        {
                            return fragmentResult;
                        }
                        path.RemoveAt(path.Count - 1);
                        break;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Determines whether the source schema <paramref name="schemaName"/> resolves every
    /// connector of <paramref name="path"/> from <paramref name="startIndex"/> down to the
    /// requirement target, whose type is <paramref name="targetType"/>.
    /// </summary>
    /// <remarks>
    /// 1. When the planner wants to fold a requirement (like an entity key) into an already-planned fetch,
    ///    it has to wrap that requirement in all the intermediate fields and fragments that lead down to it.
    ///    This method checks that the chosen source schema actually declares every one of those intermediate steps.
    /// 2. It walks the recorded path entry by entry: for each field connector,
    ///    the field must exist on its declaring type in that specific schema (and not be @external);
    ///    for each inline-fragment connector, the type condition must exist in that schema.
    /// 3. If any link in the chain fails, the whole ancestor step is rejected,
    ///    so the planner falls back to sourcing the requirement from a step that genuinely owns the path
    ///    (instead of emitting a document the subgraph would reject).
    /// </remarks>
    private static bool IsConnectorChainResolvable(
        List<(SelectionSetNode SelectionSet, ISelectionNode ConnectingNode, ITypeDefinition Type)> path,
        int startIndex,
        ITypeDefinition targetType,
        string schemaName)
    {
        for (var i = startIndex; i < path.Count; i++)
        {
            switch (path[i].ConnectingNode)
            {
                case FieldNode fieldNode:
                    if (path[i].Type is not FusionComplexTypeDefinition complexType
                        || !complexType.Fields.TryGetField(
                            fieldNode.Name.Value,
                            allowInaccessibleFields: true,
                            out var field)
                        || !field.Sources.TryGetMember(schemaName, out var source)
                        || source.IsExternal)
                    {
                        return false;
                    }
                    break;

                case InlineFragmentNode { TypeCondition: not null }:
                    // a connector's own type is the declaring type of the next connector,
                    // and for the last connector it is the type of the requirement target.
                    var typeCondition = i + 1 < path.Count ? path[i + 1].Type : targetType;

                    if (!typeCondition.ExistsInSchema(schemaName))
                    {
                        return false;
                    }
                    break;
            }
        }

        return true;
    }

    private bool TryInlineIntoAncestorStep(
        AncestorStepMatch match,
        SelectionSetNode requirements,
        SelectionPath path,
        int dependentStepId,
        SelectionSetIndexBuilder index,
        ref ImmutableList<PlanStep> steps,
        out ImmutableStack<ConditionedSelectionSet> unresolvable,
        bool treatSourceExternalAsUnresolvable = false)
    {
        unresolvable = [];

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = match.Step.SchemaName!,
            SelectionSet = new SelectionSet(
                match.TargetSelectionSetId,
                requirements,
                match.TargetType,
                path),
            SelectionSetIndex = index,
            TreatSourceExternalAsUnresolvable = treatSourceExternalAsUnresolvable
        };

        var (resolvable, partitionUnresolvable, _, _, _) = _partitioner.Partition(input);

        if (resolvable is not { Selections.Count: > 0 })
        {
            return false;
        }

        if (resolvable != requirements)
        {
            index.Register(match.TargetSelectionSetId, resolvable);
        }

        var wrappedSelections = WrapSelectionsInConnectors(match.Connectors, resolvable, index);

        var operation = InlineSelections(
            match.Step.Definition,
            index,
            match.Type,
            match.SelectionSetId,
            wrappedSelections);

        EnsureAllSelectionSetsRegistered(operation.SelectionSet, index);

        var updatedStep = match.Step with
        {
            Definition = operation,
            SelectionSets = SelectionSetIndexer.CreateIdSet(operation.SelectionSet, index),
            Dependents = match.Step.Dependents.Add(dependentStepId)
        };

        steps = steps.SetItem(match.StepIndex, updatedStep);
        unresolvable = partitionUnresolvable;

        return true;
    }

    private static SelectionSetNode WrapSelectionsInConnectors(
        List<AncestorConnector> connectors,
        SelectionSetNode selections,
        SelectionSetIndexBuilder index)
    {
        var current = selections;

        for (var i = connectors.Count - 1; i >= 0; i--)
        {
            var connector = connectors[i];
            ISelectionNode wrappedSelection = connector.Node switch
            {
                FieldNode field => field.WithSelectionSet(current),
                InlineFragmentNode fragment => fragment.WithSelectionSet(current),
                _ => throw new InvalidOperationException(
                    $"Unsupported ancestor connector '{connector.Node.Kind}'.")
            };

            current = new SelectionSetNode(
            [
                wrappedSelection
            ]);
            index.Register(connector.SelectionSetId, current);
        }

        return current;
    }

    private static void EnsureAllSelectionSetsRegistered(
        SelectionSetNode selectionSet,
        SelectionSetIndexBuilder index)
    {
        var stack = new List<SelectionSetNode>
        {
            selectionSet
        };

        while (stack.Count > 0)
        {
            var current = stack[^1];
            stack.RemoveAt(stack.Count - 1);

            if (!index.IsRegistered(current))
            {
                index.Register(current);
            }

            foreach (var selection in current.Selections)
            {
                switch (selection)
                {
                    case FieldNode { SelectionSet: { } fieldSelectionSet }:
                        stack.Add(fieldSelectionSet);
                        break;

                    case InlineFragmentNode { SelectionSet: { } fragmentSelectionSet }:
                        stack.Add(fragmentSelectionSet);
                        break;
                }
            }
        }
    }

    private readonly record struct AncestorStepMatch(
        OperationPlanStep Step,
        int StepIndex,
        uint SelectionSetId,
        ITypeDefinition Type,
        uint TargetSelectionSetId,
        ITypeDefinition TargetType,
        List<AncestorConnector> Connectors);

    private readonly record struct AncestorConnector(
        uint SelectionSetId,
        ISelectionNode Node);

    private readonly record struct PlanResult(
        OperationDefinitionNode InternalOperationDefinition,
        ImmutableList<PlanStep> Steps,
        int SearchSpace,
        int ExpandedNodes,
        int StepCount,
        PolicySlotRegistry PolicySlots);

    private readonly struct OperationStepCostState(
        int maxDepth,
        int excessFanout,
        ImmutableDictionary<int, int> opsPerLevel,
        ImmutableDictionary<int, int> stepDepths)
    {
        public int MaxDepth { get; } = maxDepth;

        public int ExcessFanout { get; } = excessFanout;

        public ImmutableDictionary<int, int> OpsPerLevel { get; } = opsPerLevel;

        public ImmutableDictionary<int, int> StepDepths { get; } = stepDepths;
    }
}

internal static class PlannerExtensions
{
    public static IEnumerable<(OperationPlanStep Step, int Index, string SchemaName)> GetCandidateSteps(
        this PlanNode current,
        uint selectionSetId)
    {
        for (var i = 0; i < current.Steps.Count; i++)
        {
            if (current.Steps[i] is OperationPlanStep step
                && step.SelectionSets.Contains(selectionSetId)
                && !string.IsNullOrEmpty(step.SchemaName))
            {
                yield return (step, i, step.SchemaName);
            }
        }
    }

    internal static ImmutableHashSet<string> GetCandidateSchemas(
        this PlanNode current,
        uint selectionSetId)
    {
        var schemaNames = ImmutableHashSet.CreateBuilder<string>();

        foreach (var (_, _, schema) in GetCandidateSteps(current, selectionSetId))
        {
            schemaNames.Add(schema);
        }

        return schemaNames.ToImmutable();
    }

    public static IReadOnlyList<Lookup> OrderLookupsDeterministically(this IEnumerable<Lookup> lookups)
    {
        return [.. lookups.OrderBy(LookupOrderingKey, StringComparer.Ordinal)];
    }

    public static string LookupOrderingKey(Lookup? lookup)
    {
        if (lookup is null)
        {
            return string.Empty;
        }

        var path = lookup.Path.Length == 0
            ? string.Empty
            : string.Join('.', lookup.Path);

        return string.Concat(
            lookup.SchemaName,
            ":",
            lookup.FieldName,
            ":",
            path,
            ":",
            lookup.Arguments.Length.ToString(),
            ":",
            lookup.Fields.Length.ToString());
    }

    public static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this FusionSchemaDefinition compositeSchema,
        SelectionSet selectionSet,
        IEnumerable<string>? additionalCandidateSchemas = null)
    {
        ArgumentNullException.ThrowIfNull(compositeSchema);
        ArgumentNullException.ThrowIfNull(selectionSet);

        var candidateSchemas = new HashSet<string>(StringComparer.Ordinal);
        var fieldResolutions = new List<FieldResolutionInfo>(selectionSet.Selections.Count);

        CollectCandidateSchemas(
            compositeSchema,
            selectionSet.Type,
            selectionSet.Selections,
            candidateSchemas);

        if (additionalCandidateSchemas is not null)
        {
            candidateSchemas.UnionWith(additionalCandidateSchemas);
        }

        CollectFieldResolutions(
            compositeSchema,
            selectionSet.Type,
            selectionSet.Selections,
            fieldResolutions);

        if (candidateSchemas.Count == 0)
        {
            yield break;
        }

        var schemas = candidateSchemas.ToArray();
        Array.Sort(schemas, StringComparer.Ordinal);

        var schemaFits = new SchemaFit[schemas.Length];
        for (var i = 0; i < schemaFits.Length; i++)
        {
            schemaFits[i] = new SchemaFit();
        }

        foreach (var fieldResolution in fieldResolutions)
        {
            for (var i = 0; i < schemas.Length; i++)
            {
                var schemaName = schemas[i];
                var fit = schemaFits[i];

                if (fieldResolution.ContainsSchema(schemaName))
                {
                    fit.Resolvable++;

                    if (fieldResolution.HasRequirements(schemaName))
                    {
                        fit.WithRequirements++;
                    }
                }
                else
                {
                    foreach (var spilloverSchema in fieldResolution.Schemas)
                    {
                        if (!spilloverSchema.Equals(schemaName, StringComparison.Ordinal))
                        {
                            fit.SpilloverSchemas.Add(spilloverSchema);
                        }
                    }
                }
            }
        }

        var rankedSchemas = new List<(string SchemaName, double Cost)>(schemas.Length);
        var totalFields = fieldResolutions.Count;

        for (var i = 0; i < schemas.Length; i++)
        {
            rankedSchemas.Add((schemas[i], schemaFits[i].ComputeCost(totalFields)));
        }

        rankedSchemas.Sort(
            static (left, right) =>
            {
                var costComparison = left.Cost.CompareTo(right.Cost);
                return costComparison != 0
                    ? costComparison
                    : string.CompareOrdinal(left.SchemaName, right.SchemaName);
            });

        foreach (var rankedSchema in rankedSchemas)
        {
            yield return rankedSchema;
        }

        static void CollectCandidateSchemas(
            FusionSchemaDefinition compositeSchema,
            ITypeDefinition type,
            IReadOnlyList<ISelectionNode> selections,
            HashSet<string> candidateSchemas)
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
                        if (fieldNode.Name.Value == IntrospectionFieldNames.TypeName)
                        {
                            // __typename is resolvable on any schema, so it normally does not
                            // constrain schema choice. The exception is an @interfaceObject-opaque
                            // position: the stand-in cannot provide an authoritative concrete
                            // __typename, so identity recovery requires a concrete-aware source
                            // (one where the interface is not a stand-in), reached through its
                            // covering interface lookup.
                            if (complexType is FusionInterfaceTypeDefinition interfaceType
                                && interfaceType.Sources.Any(t => t.IsInterfaceObject))
                            {
                                foreach (var source in interfaceType.Sources)
                                {
                                    if (!source.IsInterfaceObject)
                                    {
                                        candidateSchemas.Add(source.SchemaName);
                                    }
                                }
                            }

                            continue;
                        }

                        var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                        if (field is { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } })
                        {
                            continue;
                        }

                        if (!compositeSchema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                        {
                            continue;
                        }

                        foreach (var schemaName in fieldResolution.Schemas)
                        {
                            candidateSchemas.Add(schemaName);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        var typeCondition = type;

                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeCondition = compositeSchema.Types.GetType(
                                inlineFragmentNode.TypeCondition.Name.Value,
                                allowInaccessibleFields: true);
                        }

                        // Narrowing an @interfaceObject-opaque interface to a concrete possible type
                        // observes identity: the stand-in cannot authoritatively tell whether a
                        // value is that concrete type, so a concrete-aware source (one where the
                        // interface is not a stand-in) must recover it through its covering
                        // interface lookup. Mirrors the interface-level __typename handling above so
                        // a fragment that selects only __typename still yields a candidate schema.
                        if (complexType is FusionInterfaceTypeDefinition fragmentParentInterface
                            && typeCondition is FusionObjectTypeDefinition
                            && fragmentParentInterface.Sources.Any(t => t.IsInterfaceObject))
                        {
                            foreach (var source in fragmentParentInterface.Sources)
                            {
                                if (!source.IsInterfaceObject)
                                {
                                    candidateSchemas.Add(source.SchemaName);
                                }
                            }
                        }

                        CollectCandidateSchemas(
                            compositeSchema,
                            typeCondition,
                            inlineFragmentNode.SelectionSet.Selections,
                            candidateSchemas);
                        break;
                }
            }
        }

        static void CollectFieldResolutions(
            FusionSchemaDefinition compositeSchema,
            ITypeDefinition type,
            IReadOnlyList<ISelectionNode> selections,
            List<FieldResolutionInfo> fieldResolutions)
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
                        if (fieldNode.Name.Value == IntrospectionFieldNames.TypeName)
                        {
                            continue;
                        }

                        var field = complexType.Fields.GetField(fieldNode.Name.Value, allowInaccessibleFields: true);

                        if (field is { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } })
                        {
                            continue;
                        }

                        if (!compositeSchema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                        {
                            continue;
                        }

                        fieldResolutions.Add(fieldResolution);

                        if (fieldNode.SelectionSet is not null)
                        {
                            CollectFieldResolutions(
                                compositeSchema,
                                field.Type.AsTypeDefinition(),
                                fieldNode.SelectionSet.Selections,
                                fieldResolutions);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        var typeCondition = type;

                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeCondition = compositeSchema.Types.GetType(
                                inlineFragmentNode.TypeCondition.Name.Value,
                                allowInaccessibleFields: true);
                        }

                        CollectFieldResolutions(
                            compositeSchema,
                            typeCondition,
                            inlineFragmentNode.SelectionSet.Selections,
                            fieldResolutions);
                        break;
                }
            }
        }
    }

    private sealed class SchemaFit
    {
        public int Resolvable { get; set; }

        public int WithRequirements { get; set; }

        public HashSet<string> SpilloverSchemas { get; } = [with(StringComparer.Ordinal)];

        public double ComputeCost(int totalFields)
        {
            if (totalFields == 0)
            {
                return 0.0;
            }

            var coverageRatio = (double)Resolvable / totalFields;
            var baseCost = (1.0 - coverageRatio) * (1.0 - coverageRatio) * 20.0;
            var spilloverPenalty = SpilloverSchemas.Count * 5.0;
            var requirementPenalty = WithRequirements * 2.0;

            return baseCost + spilloverPenalty + requirementPenalty;
        }
    }

    public static int NextId(this ImmutableList<PlanStep> steps)
        => steps.LastOrDefault()?.Id + 1 ?? 1;

    /// <summary>
    /// For the given selection set on the <paramref name="workItem"/> we check if we are able
    /// to walk up the path of the selection set to find other types we can lookup on the given
    /// <paramref name="schemaName"/> or whether the entire path can be reused up until the root.
    /// </summary>
    internal static IEnumerable<(OperationWorkItem WorkItem, double Cost, ISelectionSetIndex SelectionSetIndex)>
        GetPossibleLookupsThroughPath(
            PlanNode planNodeTemplate,
            OperationWorkItem workItem,
            string schemaName,
            FusionSchemaDefinition compositeSchema)
    {
        var selectionSet = workItem.SelectionSet;

        if (selectionSet.Path.IsRoot)
        {
            yield break;
        }

        var pathItems = ReverseSelectionPath(
            planNodeTemplate.InternalOperationDefinition,
            selectionSet.Path,
            compositeSchema,
            planNodeTemplate.SelectionSetIndex,
            selectionSet.Id);

        if (pathItems is null)
        {
            yield break;
        }

        var selectionSetIndexBuilder = planNodeTemplate.SelectionSetIndex.ToBuilder();
        var path = selectionSet.Path;
        var segmentLength = path.Length;
        var finalSelectionSet = selectionSet.Node;
        var fieldsMovedUp = 0;
        var viewerFallbackToQueryRoot = false;

        while (pathItems.TryPop(out var pathItem))
        {
            if (pathItem is FieldPathItem fieldPathItem)
            {
                if (!compositeSchema.TryGetFieldResolution(
                        fieldPathItem.Field.DeclaringType,
                        fieldPathItem.Field.Name,
                        out var fieldResolution)
                    || !fieldResolution.ContainsSchema(schemaName))
                {
                    if (planNodeTemplate.OperationDefinition.Operation != OperationType.Query
                        && IsViewerFieldSelection(fieldPathItem.Node)
                        && HasViewerQueryRoot(schemaName, compositeSchema))
                    {
                        finalSelectionSet = new SelectionSetNode(
                            [fieldPathItem.Node.WithSelectionSet(finalSelectionSet)]);
                        selectionSetIndexBuilder.Register(
                            planNodeTemplate.InternalOperationDefinition.SelectionSet,
                            finalSelectionSet);
                        fieldsMovedUp++;
                        viewerFallbackToQueryRoot = true;
                        break;
                    }

                    yield break;
                }

                finalSelectionSet = new SelectionSetNode(
                    [fieldPathItem.Node.WithSelectionSet(finalSelectionSet)]);

                fieldsMovedUp++;
            }
            else if (pathItem is InlineFragmentPathItem inlineFragmentPathItem)
            {
                if (inlineFragmentPathItem.TypeCondition is { } typeCondition
                    && !typeCondition.ExistsInSchema(schemaName))
                {
                    yield break;
                }

                finalSelectionSet = new SelectionSetNode(
                    [inlineFragmentPathItem.Node.WithSelectionSet(finalSelectionSet)]);
            }

            if (pathItem is not InlineFragmentPathItem { TypeCondition: null })
            {
                segmentLength--;
            }

            if (pathItems.TryPeek(out var parentPathItem))
            {
                var parentSelectionSet = parentPathItem switch
                {
                    FieldPathItem f => f.Node.SelectionSet!,
                    InlineFragmentPathItem f => f.Node.SelectionSet,
                    _ => throw new InvalidOperationException(
                        "Expected parent selection to either be field or inline fragment")
                };

                selectionSetIndexBuilder.Register(parentSelectionSet, finalSelectionSet);

                var parentType = parentPathItem switch
                {
                    FieldPathItem f => f.Field.Type.NamedType<IOutputTypeDefinition>(),
                    InlineFragmentPathItem f => f.TypeCondition,
                    _ => null
                };

                if (parentType?.ExistsInSchema(schemaName) == true)
                {
                    foreach (var lookup in compositeSchema
                        .GetPossibleLookupsOrdered(parentType, schemaName))
                    {
                        var newSelectionSet = new SelectionSet(
                            selectionSetIndexBuilder.GetId(finalSelectionSet),
                            finalSelectionSet,
                            parentType,
                            path.Slice(segmentLength));

                        var newWorkItem = workItem with { SelectionSet = newSelectionSet, Lookup = lookup };

                        yield return (newWorkItem, fieldsMovedUp, selectionSetIndexBuilder);
                    }
                }
            }
            else
            {
                selectionSetIndexBuilder.Register(
                    planNodeTemplate.InternalOperationDefinition.SelectionSet,
                    finalSelectionSet);
            }
        }

        // For mutations/subscriptions we generally avoid query-root fallback to prevent
        // duplicate root operations. The viewer convention is the one supported exception,
        // because cross-subgraph viewer fields are resolved via Query.viewer.
        if (planNodeTemplate.OperationDefinition.Operation != OperationType.Query
            && !IsViewerRootSelection(finalSelectionSet))
        {
            yield break;
        }

        var newRootSelectionSet = new SelectionSet(
            selectionSetIndexBuilder.GetId(finalSelectionSet),
            finalSelectionSet,
            compositeSchema.QueryType,
            viewerFallbackToQueryRoot ? selectionSet.Path : SelectionPath.Root);

        var newRootWorkItem = workItem with { Kind = OperationWorkItemKind.Root, SelectionSet = newRootSelectionSet };

        yield return (newRootWorkItem, fieldsMovedUp, selectionSetIndexBuilder);
    }

    private static bool IsViewerRootSelection(SelectionSetNode selectionSet)
        => selectionSet.Selections is [FieldNode field] && IsViewerFieldSelection(field);

    internal static bool IsViewerFieldSelection(FieldNode field)
        => field is
        {
            Name.Value: "viewer",
            Alias: null,
            Arguments.Count: 0,
            Directives.Count: 0
        };

    private static bool HasViewerQueryRoot(
        string schemaName,
        FusionSchemaDefinition compositeSchema)
        => compositeSchema.TryGetFieldResolution(
            compositeSchema.QueryType,
            "viewer",
            out var viewerResolution)
            && viewerResolution.ContainsSchema(schemaName);

    /// <summary>
    /// Reverses a selection path into the concrete field and inline fragment selections that
    /// connect the operation root to the terminal selection set.
    /// </summary>
    /// <param name="operationDefinitionNode">The operation definition that owns the path.</param>
    /// <param name="path">The selection path to reverse.</param>
    /// <param name="compositeSchema">The composite schema used to resolve field and type metadata.</param>
    /// <param name="selectionSetIndex">The selection set index for the operation definition.</param>
    /// <param name="terminalSelectionSetId">The logical identifier of the terminal selection set.</param>
    /// <returns>
    /// A stack of path items from the terminal selection set back to the operation root, or
    /// <c>null</c> when the path cannot be resolved.
    /// </returns>
    private static Stack<IPathItem>? ReverseSelectionPath(
        OperationDefinitionNode operationDefinitionNode,
        SelectionPath path,
        FusionSchemaDefinition compositeSchema,
        ISelectionSetIndex selectionSetIndex,
        uint terminalSelectionSetId)
    {
        IOutputTypeDefinition currentType = compositeSchema.GetOperationType(operationDefinitionNode.Operation);
        var currentSelectionSetNode = operationDefinitionNode.SelectionSet;

        var items = new Stack<IPathItem>();

        for (var i = 0; i < path.Length; i++)
        {
            var segment = path[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field:
                    var fieldAliasOrName = segment.Name;

                    var fieldSelection = FindThroughAnonymousFragments<FieldNode>(
                        currentSelectionSetNode,
                        f => (f.Name.Value == fieldAliasOrName || f.Alias?.Value == fieldAliasOrName)
                            && ChildSelectionSetIdMatches(
                                f,
                                selectionSetIndex,
                                terminalSelectionSetId),
                        items);

                    fieldSelection ??= FindThroughAnonymousFragments<FieldNode>(
                        currentSelectionSetNode,
                        f => f.Name.Value == fieldAliasOrName || f.Alias?.Value == fieldAliasOrName,
                        items);

                    if (fieldSelection is null)
                    {
                        return null;
                    }

                    if (currentType is not FusionComplexTypeDefinition complexType
                        || !complexType.Fields.TryGetField(fieldSelection.Name.Value, allowInaccessibleFields: true, out var field))
                    {
                        return null;
                    }

                    currentType = field.Type.NamedType<IOutputTypeDefinition>();
                    currentSelectionSetNode = fieldSelection.SelectionSet!;

                    items.Push(new FieldPathItem(fieldSelection, field));

                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    var inlineFragmentSelection = FindThroughAnonymousFragments<InlineFragmentNode>(
                        currentSelectionSetNode,
                        f => f.TypeCondition?.Name.Value == segment.Name,
                        items);

                    if (inlineFragmentSelection is null)
                    {
                        return null;
                    }

                    IOutputTypeDefinition? typeCondition = null;
                    if (inlineFragmentSelection.TypeCondition?.Name.Value is { } typeConditionName)
                    {
                        if (!compositeSchema.Types.TryGetType(
                            typeConditionName,
                            allowInaccessibleFields: true,
                            out typeCondition))
                        {
                            return null;
                        }

                        currentType = typeCondition;
                    }

                    currentSelectionSetNode = inlineFragmentSelection.SelectionSet;

                    items.Push(new InlineFragmentPathItem(inlineFragmentSelection, typeCondition));

                    break;

                default:
                    throw new NotImplementedException($"Segment kind {segment.Kind} is not supported.");
            }
        }

        return items;
    }

    /// <summary>
    /// Determines whether a field's child selection set has the expected logical identifier.
    /// </summary>
    /// <param name="field">The field whose child selection set should be matched.</param>
    /// <param name="index">The selection set index that assigns logical identifiers.</param>
    /// <param name="terminalSelectionSetId">The logical identifier to match against.</param>
    /// <returns>
    /// <c>true</c> when the field's child selection set matches
    /// <paramref name="terminalSelectionSetId"/>, otherwise <c>false</c>.
    /// </returns>
    private static bool ChildSelectionSetIdMatches(
        FieldNode field,
        ISelectionSetIndex index,
        uint terminalSelectionSetId)
    {
        if (field.SelectionSet is not { } childSelectionSet
            || !index.IsRegistered(childSelectionSet))
        {
            return false;
        }

        var childId = index.GetId(childSelectionSet);

        if (childId == terminalSelectionSetId)
        {
            return true;
        }

        // account for cloned selection-set ids in either direction
        if (index.TryGetOriginalIdFromCloned(terminalSelectionSetId, out var originalTerminal)
            && childId == originalTerminal)
        {
            return true;
        }

        if (index.TryGetOriginalIdFromCloned(childId, out var originalChild)
            && originalChild == terminalSelectionSetId)
        {
            return true;
        }

        return false;
    }

    private static T? FindThroughAnonymousFragments<T>(
        SelectionSetNode selectionSetNode,
        Func<T, bool> predicate,
        Stack<IPathItem> items) where T : class, ISelectionNode
    {
        foreach (var selection in selectionSetNode.Selections)
        {
            if (selection is T candidate && predicate(candidate))
            {
                return candidate;
            }
        }

        foreach (var selection in selectionSetNode.Selections)
        {
            if (selection is InlineFragmentNode { TypeCondition: null } anonymousFragment)
            {
                items.Push(new InlineFragmentPathItem(anonymousFragment, null));

                var found = FindThroughAnonymousFragments(
                    anonymousFragment.SelectionSet, predicate, items);

                if (found is not null)
                {
                    return found;
                }

                items.Pop();
            }
        }

        return null;
    }

    private interface IPathItem;

    private record FieldPathItem(FieldNode Node, FusionOutputFieldDefinition Field) : IPathItem;

    private record InlineFragmentPathItem(
        InlineFragmentNode Node,
        IOutputTypeDefinition? TypeCondition) : IPathItem;
}

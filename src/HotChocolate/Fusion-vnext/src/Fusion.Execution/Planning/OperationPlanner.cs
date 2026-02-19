using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
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
    private readonly OperationPlannerOptions _options;

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
        _options = options;
    }

    internal OperationPlannerOptions Options => _options;

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
            // We first need to create an index to keep track of the logical selections
            // sets before we can branch them. This allows us to inline requirements later
            // into the right place.
            var index = SelectionSetIndexer.Create(operationDefinition);

            // Next, we create the seed plan with a set of initial work items exploring the root selection set.
            var (node, selectionSet) = operationDefinition.Operation switch
            {
                OperationType.Query => CreateQueryPlanBase(operationDefinition, shortHash, index),
                OperationType.Mutation => CreateMutationPlanBase(operationDefinition, shortHash, index),
                OperationType.Subscription => CreateSubscriptionPlanBase(operationDefinition, shortHash, index),
                _ => throw new ArgumentOutOfRangeException()
            };

            var internalOperationDefinition = operationDefinition;
            ImmutableList<PlanStep> planSteps = [];

            // The backlog is only empty for pure introspection queries, which the
            // gateway serves directly without planning against any source schema.
            if (!node.Backlog.IsEmpty)
            {
                var possiblePlans = new PlanQueue(_schema);

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

                // For plans that cannot be branched we simply enqueue the plan node.
                // This often happens when we have computed fields like `node` which will be
                // expanded later.
                if (possiblePlans.Count < 1)
                {
                    possiblePlans.Enqueue(node);
                }

                // Now that we have seeded the possible plans we can start planning.
                var plan = Plan(id, possiblePlans, eventSourceEnabled, cancellationToken);

                if (!plan.HasValue)
                {
                    throw new InvalidOperationException("No possible plan was found.");
                }

                internalOperationDefinition = plan.Value.InternalOperationDefinition;
                planSteps = plan.Value.Steps;
                searchSpace = plan.Value.SearchSpace;
                expandedNodes = plan.Value.ExpandedNodes;
                stepCount = plan.Value.StepCount;

                internalOperationDefinition =
                    AddTypeNameToAbstractSelections(
                        internalOperationDefinition,
                        _schema.GetOperationType(operationDefinition.Operation));
            }

            var operation = _operationCompiler.Compile(id, hash, internalOperationDefinition);
            var operationPlan = BuildExecutionPlan(
                operation,
                operationDefinition,
                planSteps,
                searchSpace,
                expandedNodes);

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

    private PlanResult? Plan(
        string operationId,
        PlanQueue possiblePlans,
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
        var bestCompletePlan = TryBuildGreedyCompletePlan(possiblePlans, cancellationToken);
        var bestCompletePlanCost = bestCompletePlan is null ? double.PositiveInfinity : bestCompletePlan.PathCost;

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
                    PlanRootSelections(wi, current, backlog, possiblePlans);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, possiblePlans);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        possiblePlans,
                        backlog);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        possiblePlans,
                        backlog);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, possiblePlans, backlog);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, possiblePlans, backlog);
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

        return new PlanResult(
            bestCompletePlan.InternalOperationDefinition,
            bestCompletePlan.Steps,
            searchSpace,
            expandedNodes,
            bestCompletePlan.OperationStepCount);

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

    private PlanNode? TryBuildGreedyCompletePlan(PlanQueue possiblePlans, CancellationToken cancellationToken)
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
                    PlanRootSelections(wi, current, backlog, candidates);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, candidates);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        candidates,
                        backlog);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        candidates,
                        backlog);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, candidates, backlog);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, candidates, backlog);
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

    private void PlanRootSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Backlog backlog,
        PlanQueue possiblePlans)
        => PlanSelections(workItem, current, null, backlog, possiblePlans);

    private void PlanLookupSelections(
        OperationWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        Backlog backlog,
        PlanQueue possiblePlans)
    {
        current = InlineLookupRequirements(
            workItem.SelectionSet,
            current,
            lookup,
            workItem.EstimatedDepth,
            backlog);
        PlanSelections(
            workItem,
            current,
            lookup,
            current.Backlog,
            possiblePlans);
    }

    private void PlanSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Lookup? lookup,
        Backlog backlog,
        PlanQueue possiblePlans)
    {
        var stepId = current.Steps.NextId();
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = workItem.SelectionSet,
            SelectionSetIndex = index
        };

        (var resolvable, var unresolvable, var fieldsWithRequirements, index) = _partitioner.Partition(input);

        // if we cannot resolve any selection with the current source schema then this path
        // cannot be used to resolve the data for the current operation, and we need to skip it.
        if (resolvable is null)
        {
            return;
        }

        backlog = backlog.PushUnresolvable(unresolvable, current.SchemaName, stepDepth);
        backlog = backlog.PushRequirements(fieldsWithRequirements, stepId, stepDepth);

        // lookups are always queries.
        var operationType =
            lookup is null
                ? current.OperationDefinition.Operation
                : OperationType.Query;

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
                    fieldSelectionMap);

                requirements = requirements.Add(argumentRequirementKey, operationRequirement);
            }

            operationBuilder.SetLookup(lookup, GetLookupArguments(lookup, requirementKey), workItem.SelectionSet.Type);
        }

        (var definition, index, var source) = operationBuilder.Build(index);

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(resolvable),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Dependents = workItem.Dependents,
            Requirements = requirements,
            Target = workItem.SelectionSet.Path,
            Source = source,
            Lookup = lookup
        };

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
            Steps = current.Steps.Add(step),
            LastRequirementId = lastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.EnqueueBranches(next);
    }

    private PlanNode InlineLookupRequirements(
        SelectionSet workItemSelectionSet,
        PlanNode current,
        Lookup lookup,
        int lookupStepDepth,
        Backlog backlog)
    {
        var processed = new HashSet<string>();
        var lookupStepId = current.Steps.NextId();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.Requirements;

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
            if (!processed.Add(schemaName) || lookup.SchemaName.Equals(schemaName))
            {
                continue;
            }

            var input = new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                SelectionSet = workItemSelectionSet with { Node = selectionSet },
                SelectionSetIndex = index
            };

            var (resolvable, unresolvable, _, _) = _partitioner.Partition(input);

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
                    if (top.Id == workItemSelectionSet.Id)
                    {
                        unresolvable = unresolvable.Pop(out top);
                        selectionSet = top.Node;
                    }

                    backlog = backlog.PushUnresolvable(
                        unresolvable,
                        current.SchemaName,
                        GetOperationStepDepth(current, step.Id));
                }
            }

            if (selectionSet is null)
            {
                break;
            }
        }

        // if we have still selections left we need to add them to the backlog.
        if (selectionSet is not null)
        {
            backlog = backlog.Push(
                new OperationWorkItem(
                    OperationWorkItemKind.Lookup,
                    workItemSelectionSet with { Node = selectionSet },
                    FromSchema: lookup.SchemaName)
                {
                    Dependents = ImmutableHashSet<int>.Empty.Add(lookupStepId),
                    ParentDepth = lookupStepDepth
                });
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

    /// <summary>
    /// Tries to inline the field that has a requirement into its original intended plan step
    /// by resolving the requirements from non-dependant siblings or from parents.
    /// </summary>
    private void PlanInlineFieldWithRequirements(
        FieldRequirementWorkItem workItem,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog)
    {
        // we first resolve the original intended plan step, so we can inline the field
        // into it.
        if (current.Steps.ById(workItem.StepId) is not OperationPlanStep currentStep)
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
                    ref current,
                    currentStep,
                    index,
                    ref backlog,
                    ref steps)
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

        var operation =
            InlineSelections(
                currentStep.Definition,
                index,
                currentStep.Type,
                workItem.Selection.SelectionSetId,
                new SelectionSetNode([workItem.Selection.Node.WithArguments(arguments)]));

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
                fieldSelectionMap);

            requirements = requirements.Add(argumentRequirementKey, operationRequirement);
        }

        var updatedStep = currentStep with { Definition = operation, Requirements = requirements };

        steps = steps.SetItem(workItem.StepIndex, updatedStep);
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
            OperationStepCount = current.OperationStepCount,
            MaxDepth = current.MaxDepth,
            ExcessFanout = current.ExcessFanout,
            OpsPerLevel = current.OpsPerLevel,
            OperationStepDepths = current.OperationStepDepths
        };

        possiblePlans.EnqueueBranches(next);
    }

    private void PlanFieldWithRequirement(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog)
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
            backlog);
        backlog = current.Backlog;

        if (current.Steps.ById(workItem.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        var steps = current.Steps;
        var stepId = current.Steps.NextId();
        var stepDepth = workItem.EstimatedDepth;
        var indexBuilder = current.SelectionSetIndex.ToBuilder();
        var lastRequirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{lastRequirementId}";
        var requirements = ImmutableDictionary<string, OperationRequirement>.Empty;

        var leftoverRequirements =
            TryInlineFieldRequirements(
                workItem,
                ref current,
                currentStep,
                indexBuilder,
                ref backlog,
                ref steps);

        // if we have requirements that we could not inline into existing
        // nodes of the operation plan we will put it on the backlog to be
        // planned as another lookup.
        if (leftoverRequirements is not null)
        {
            indexBuilder.Register(
                workItem.Selection.SelectionSetId,
                leftoverRequirements);

            backlog = backlog.Push(
                new OperationWorkItem(
                    OperationWorkItemKind.Lookup,
                    new SelectionSet(
                        workItem.Selection.SelectionSetId,
                        leftoverRequirements,
                        workItem.Selection.Field.DeclaringType,
                        workItem.Selection.Path),
                    FromSchema: lookup.SchemaName)
                {
                    Dependents = ImmutableHashSet<int>.Empty.Add(stepId),
                    ParentDepth = stepDepth
                });
        }

        var compositeField = workItem.Selection.Field;
        var sourceField = compositeField.Sources[current.SchemaName];
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
                fieldSelectionMap);

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

        var childSelections =
            ExtractResolvableChildSelections(
                stepId,
                stepDepth,
                workItem.Selection,
                current,
                indexBuilder,
                ref backlog);

        var selectionSetNode = new SelectionSetNode(
            [workItem.Selection.Node.WithArguments(arguments).WithSelectionSet(childSelections)]);
        indexBuilder.Register(workItem.Selection.SelectionSetId, selectionSetNode);

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
                fieldSelectionMap);

            requirements = requirements.Add(argumentRequirementKey, operationRequirement);
        }

        operationBuilder.SetLookup(
            lookup,
            GetLookupArguments(lookup, requirementKey),
            workItem.Selection.Field.DeclaringType);

        var (definition, index, source) = operationBuilder.Build(indexBuilder);

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = compositeField.DeclaringType,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(selectionSetNode),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, indexBuilder),
            Requirements = requirements,
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
            Steps = steps.Add(step),
            LastRequirementId = lastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.EnqueueBranches(next);
    }

    private void PlanNodeLookup(
        NodeLookupWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog)
    {
        var stepId = current.Steps.NextId();
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = workItem.SelectionSet,
            SelectionSetIndex = index
        };

        (var resolvable, var unresolvable, var fieldsWithRequirements, index) = _partitioner.Partition(input);

        // if we cannot resolve any selection with the current source schema then this path
        // cannot be used to resolve the data for the current operation, and we need to skip it.
        if (resolvable is null)
        {
            return;
        }

        backlog = backlog.PushUnresolvable(unresolvable, current.SchemaName, stepDepth);
        backlog = backlog.PushRequirements(fieldsWithRequirements, stepId, stepDepth);

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

        var operationPlanStep = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(resolvable),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Dependents = workItem.Dependents,
#if NET10_0_OR_GREATER
            Requirements = [],
#else
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
#endif
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
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.EnqueueBranches(next);
    }

    private void PlanNode(
        NodeFieldWorkItem workItem,
        PlanNode current,
        PlanQueue possiblePlans,
        Backlog backlog)
    {
        var stepId = current.Steps.NextId();
        var fallbackQueryStepId = stepId + 1;
        var stepDepth = workItem.EstimatedDepth;
        var index = current.SelectionSetIndex;
        var nodeField = workItem.NodeField.Field;
        var responseName = nodeField.Alias?.Value ?? nodeField.Name.Value;
        var selectionPath = SelectionPath.Root.AppendField(responseName);

        var idArgumentValue = nodeField.Arguments.First(a => a.Name.Value == "id").Value;

        var selectionSet = new SelectionSet(
            index.GetId(nodeField.SelectionSet!),
            nodeField.SelectionSet!,
            _schema.Types["Node"],
            selectionPath);

        var input = new SelectionSetByTypePartitionerInput { SelectionSet = selectionSet, SelectionSetIndex = index };

        (var sharedSelectionSet, var selectionSetsByType, index) = _selectionSetByTypePartitioner.Partition(input);

        var sharedSelections = sharedSelectionSet?.Selections ?? [];
        if (sharedSelections.Count < 1 || !sharedSelections.Any(IsTypeNameSelection))
        {
            sharedSelections = [new FieldNode(IntrospectionFieldNames.TypeName), .. sharedSelections];
        }

        var nodeFieldSelectionSet = new SelectionSetNode(sharedSelections);
        var nodeFieldWithSelectionSet = nodeField.WithSelectionSet(nodeFieldSelectionSet);
        var fallbackQuerySelectionSet = new SelectionSetNode([nodeFieldWithSelectionSet]);

        var fallbackQueryBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetName(current.CreateOperationName(fallbackQueryStepId))
                .SetSelectionSet(fallbackQuerySelectionSet);

        (var fallbackQuery, index, _) = fallbackQueryBuilder.Build(index);

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
            Dependents = [],
#if NET10_0_OR_GREATER
            Requirements = [],
#else
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
#endif
            Target = SelectionPath.Root,
            Source = SelectionPath.Root
        };

        var nodeStep = new NodeFieldPlanStep
        {
            Id = stepId,
            ResponseName = responseName,
            IdValue = idArgumentValue,
            Conditions = ExtractConditions(workItem.NodeField),
            FallbackQuery = fallbackQueryStep
        };

        foreach (var (type, selectionSetNode) in selectionSetsByType)
        {
            var nodeSelectionSet = new SelectionSet(
                index.GetId(selectionSetNode),
                selectionSetNode,
                type,
                selectionPath.AppendFragment(type.Name));

            var newWorkItem = new NodeLookupWorkItem(
                Lookup: null,
                responseName,
                idArgumentValue,
                nodeSelectionSet)
            {
                ParentDepth = stepDepth
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
            Steps = current.Steps
                .Add(nodeStep)
                .Add(fallbackQueryStep),
            LastRequirementId = current.LastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
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
        ref Backlog backlog)
    {
        if (selection.Node.SelectionSet is null)
        {
            return null;
        }

        var selectionSetId = index.GetId(selection.Node.SelectionSet);
        var selectionSetType = selection.Field.Type.AsTypeDefinition();
        var selectionSet = new SelectionSet(
            selectionSetId,
            selection.Node.SelectionSet,
            selectionSetType,
            selection.Path);

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = selectionSet,
            SelectionSetIndex = index
        };

        var (resolvable, unresolvable, fieldsWithRequirements, _) = _partitioner.Partition(input);
        backlog = backlog.PushUnresolvable(unresolvable, current.SchemaName, stepDepth);
        backlog = backlog.PushRequirements(fieldsWithRequirements, stepId, stepDepth);
        return resolvable;
    }

    private SelectionSetNode? TryInlineFieldRequirements(
        FieldRequirementWorkItem workItem,
        ref PlanNode current,
        OperationPlanStep currentStep,
        SelectionSetIndexBuilder index,
        ref Backlog backlog,
        ref ImmutableList<PlanStep> steps)
    {
        var compositeField = workItem.Selection.Field;
        var sourceField = compositeField.Sources[current.SchemaName];
        var requirements = sourceField.Requirements!.Requirements;

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

        index.Register(
            workItem.Selection.SelectionSetId,
            requirements);

        var internalOperation =
            InlineSelectionsIntoOverallOperation(
                current.InternalOperationDefinition,
                index,
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.SelectionSetId,
                requirements);
        current = current with { InternalOperationDefinition = internalOperation };

        foreach (var (step, stepIndex, schemaName) in current.GetCandidateSteps(workItem.Selection.SelectionSetId))
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

            index.Register(workItem.Selection.SelectionSetId, requirements);

            var input = new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                SelectionSet = new SelectionSet(
                    workItem.Selection.SelectionSetId,
                    requirements,
                    workItem.Selection.Field.DeclaringType,
                    workItem.Selection.Path),
                SelectionSetIndex = index
            };

            var (resolvable, unresolvable, _, _) = _partitioner.Partition(input);

            if (resolvable is not { Selections.Count: > 0 })
            {
                // if we cannot resolve any selection with the current source we cannot inline the
                // field requirements into this step.
                continue;
            }

            // the resolvable part of the requirement could be different from the requirement
            // if we are unable to inline the complete requirement into a single plan step.
            // in this case we will register the resolvable part as part of the requirements selection set
            // so that they logically belong together.
            if (resolvable != requirements)
            {
                index.Register(workItem.Selection.SelectionSetId, resolvable);
            }

            var operation =
                InlineSelections(
                    step.Definition,
                    index,
                    workItem.Selection.Field.DeclaringType,
                    workItem.Selection.SelectionSetId,
                    resolvable);

            var updatedStep = step with
            {
                Definition = operation,

                // the step containing the field requirements is now dependent on this step.
                Dependents = step.Dependents.Add(workItem.StepId)
            };

            steps = steps.SetItem(stepIndex, updatedStep);
            requirements = null;

            if (!unresolvable.IsEmpty)
            {
                // if we have unresolvable parts of the requirements we will take the top level
                // parts that are not resolvable and try to resolve them in the next iteration.
                // Unresolvable child selections are pushed to the backlog and will be processed
                // in a later planing iteration.
                var top = unresolvable.Peek();
                if (top.Id == workItem.Selection.SelectionSetId)
                {
                    unresolvable = unresolvable.Pop(out top);
                    requirements = top.Node;
                }

                foreach (var selectionSet in unresolvable.Reverse())
                {
                    backlog = backlog.Push(
                        new OperationWorkItem(
                            OperationWorkItemKind.Lookup,
                            selectionSet,
                            FromSchema: current.SchemaName)
                        {
                            ParentDepth = GetOperationStepDepth(current, step.Id)
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

        return requirements;
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

        return InlineSelections(
            operation,
            index,
            selectionSetType,
            targetSelectionSetId,
            selectionsToInline,
            inlineInternal: true);
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
                        var directives = AddInternalDirective(selection);

                        switch (selection)
                        {
                            case FieldNode field:
                                selections.Add(field.WithDirectives(directives));
                                IndexInternalSelections(field.SelectionSet, index, ref backlog);
                                break;

                            case InlineFragmentNode inlineFragment:
                                selections.Add(inlineFragment.WithDirectives(directives));
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

        static IReadOnlyList<DirectiveNode> AddInternalDirective(IHasDirectives selection)
        {
            var directives = new List<DirectiveNode>(selection.Directives.Count + 1);

            if (selection.Directives.Count > 0)
            {
                directives.AddRange(selection.Directives);
            }

            directives.Add(new DirectiveNode("fusion__requirement"));

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
                    path.Push(_schema.Types[typeCondition.Name.Value]);
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

    private readonly record struct PlanResult(
        OperationDefinitionNode InternalOperationDefinition,
        ImmutableList<PlanStep> Steps,
        int SearchSpace,
        int ExpandedNodes,
        int StepCount);

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
        SelectionSet selectionSet)
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
                            typeCondition = compositeSchema.Types[inlineFragmentNode.TypeCondition.Name.Value];
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
                            typeCondition = compositeSchema.Types[inlineFragmentNode.TypeCondition.Name.Value];
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

        public HashSet<string> SpilloverSchemas { get; } = new(StringComparer.Ordinal);

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
            compositeSchema);

        if (pathItems is null)
        {
            yield break;
        }

        var selectionSetIndexBuilder = planNodeTemplate.SelectionSetIndex.ToBuilder();
        var segments = selectionSet.Path.Segments;
        var finalSelectionSet = selectionSet.Node;
        var fieldsMovedUp = 0;

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

            segments = segments.RemoveAt(segments.Length - 1);

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
                            SelectionPath.From(segments));

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

        // Even if we can walk up to the root of a non-Query operation,
        // we want to bail here as we do not want two nodes with the same root fields.
        if (planNodeTemplate.OperationDefinition.Operation != OperationType.Query)
        {
            yield break;
        }

        var newRootSelectionSet = new SelectionSet(
            selectionSetIndexBuilder.GetId(finalSelectionSet),
            finalSelectionSet,
            compositeSchema.QueryType,
            SelectionPath.Root);

        var newRootWorkItem = workItem with { Kind = OperationWorkItemKind.Root, SelectionSet = newRootSelectionSet };

        yield return (newRootWorkItem, fieldsMovedUp, selectionSetIndexBuilder);
    }

    private static Stack<IPathItem>? ReverseSelectionPath(
        OperationDefinitionNode operationDefinitionNode,
        SelectionPath path,
        FusionSchemaDefinition compositeSchema)
    {
        IOutputTypeDefinition currentType = compositeSchema.QueryType;
        var currentSelectionSetNode = operationDefinitionNode.SelectionSet;

        var items = new Stack<IPathItem>();

        foreach (var segment in path.Segments)
        {
            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Root or SelectionPathSegmentKind.Field:
                    var fieldAliasOrName = segment.Name;

                    var fieldSelection = currentSelectionSetNode.Selections
                        .OfType<FieldNode>()
                        .FirstOrDefault(f => f.Name.Value == fieldAliasOrName || f.Alias?.Value == fieldAliasOrName);

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
                    var inlineFragmentSelection = currentSelectionSetNode.Selections
                        .OfType<InlineFragmentNode>()
                        .FirstOrDefault(f => f.TypeCondition?.Name.Value == segment.Name);

                    if (inlineFragmentSelection is null)
                    {
                        return null;
                    }

                    IOutputTypeDefinition? typeCondition = null;
                    if (inlineFragmentSelection.TypeCondition?.Name.Value is { } typeConditionName)
                    {
                        if (!compositeSchema.Types.TryGetType(typeConditionName, out typeCondition))
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

    private interface IPathItem;

    private record FieldPathItem(FieldNode Node, FusionOutputFieldDefinition Field) : IPathItem;

    private record InlineFragmentPathItem(
        InlineFragmentNode Node,
        IOutputTypeDefinition? TypeCondition) : IPathItem;
}

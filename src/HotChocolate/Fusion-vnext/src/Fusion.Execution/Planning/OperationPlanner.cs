using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
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
    /// <returns>The operation plan.</returns>
    public OperationPlan CreatePlan(
        string id,
        string hash,
        string shortHash,
        OperationDefinitionNode operationDefinition)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrEmpty(hash);
        ArgumentException.ThrowIfNullOrEmpty(shortHash);
        ArgumentNullException.ThrowIfNull(operationDefinition);

        var eventSource = PlannerEventSource.Log;
        var eventSourceEnabled = eventSource.IsEnabled();
        var operationType = operationDefinition.Operation.ToString();
        var rootSelectionCount = operationDefinition.SelectionSet.Selections.Count;
        var startedAt = eventSourceEnabled ? Stopwatch.GetTimestamp() : 0L;
        var searchSpace = 0u;
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

            var (node, selectionSet) = operationDefinition.Operation switch
            {
                OperationType.Query => CreateQueryPlanBase(operationDefinition, shortHash, index),
                OperationType.Mutation => CreateMutationPlanBase(operationDefinition, shortHash, index),
                OperationType.Subscription => CreateSubscriptionPlanBase(operationDefinition, shortHash, index),
                _ => throw new ArgumentOutOfRangeException()
            };

            var internalOperationDefinition = operationDefinition;
            ImmutableList<PlanStep> planSteps = [];

            if (!node.Backlog.IsEmpty)
            {
                var possiblePlans = new PriorityQueue<PlanNode, double>();

                foreach (var (schemaName, resolutionCost) in _schema.GetPossibleSchemas(selectionSet))
                {
                    possiblePlans.EnqueueWithCost(
                        node with
                        {
                            SchemaName = schemaName,
                            ResolutionCost = resolutionCost
                        },
                        _schema);
                }

                if (possiblePlans.Count < 1)
                {
                    possiblePlans.EnqueueWithCost(node, _schema);
                }

                var plan = Plan(id, possiblePlans, eventSourceEnabled);

                if (!plan.HasValue)
                {
                    throw new InvalidOperationException("No possible plan was found.");
                }

                internalOperationDefinition = plan.Value.InternalOperationDefinition;
                planSteps = plan.Value.Steps;
                searchSpace = plan.Value.SearchSpace;
                expandedNodes = plan.Value.ExpandedNodes;
                stepCount = plan.Value.StepCount;

                internalOperationDefinition = AddTypeNameToAbstractSelections(
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
                    searchSpace <= int.MaxValue ? (int)searchSpace : int.MaxValue,
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

    private (PlanNode Node, SelectionSet First) CreateQueryPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            _schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var input = new RootSelectionSetPartitionerInput { SelectionSet = selectionSet, SelectionSetIndex = index };
        var result = _nodeFieldSelectionSetPartitioner.Partition(input);

        var backlog = ImmutableStack<WorkItem>.Empty;
        var backlogCostState = BacklogCostState.Empty;

        if (result.SelectionSet is not null)
        {
            var workItem = OperationWorkItem.CreateRoot(result.SelectionSet);
            backlog = backlog.PushWithLowerBound(workItem, ref backlogCostState);
        }

        if (result.NodeFields is not null)
        {
            foreach (var nodeField in result.NodeFields)
            {
                var workItem = new NodeFieldWorkItem(nodeField);
                backlog = backlog.PushWithLowerBound(workItem, ref backlogCostState);
            }
        }

        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlogCostState);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = "None",
            Options = _options,
            SelectionSetIndex = result.SelectionSetIndex,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            OperationStepCount = 0
        };

        return (node, selectionSet);
    }

    private (PlanNode Node, SelectionSet First) CreateMutationPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        // todo: we need to do this with a rewriter as in this case we are not
        // dealing with fragments.

        // For mutations, we slice the root selection set into individual root selections,
        // so that we can plan each root selection separately. This aligns with the
        // GraphQL mutation execution algorithm where mutation fields at the root level
        // must be executed sequentially: execute the first mutation field and all its
        // child selections (which represent the query of the mutation's affected state),
        // then move to the next mutation field and repeat.
        //
        // The plan will end up with separate root nodes for each mutation field, and the
        // plan executor will execute these root nodes in document order.
        var backlog = ImmutableStack<WorkItem>.Empty;
        var backlogCostState = BacklogCostState.Empty;
        var selectionSetId = index.GetId(operationDefinition.SelectionSet);
        var indexBuilder = index.ToBuilder();
        SelectionSet firstSelectionSet = null!;

        // We traverse in reverse order and push to the stack so that the first mutation
        // field (index 0) will end up on top of the stack and be processed first.
        // Due to LIFO stack behavior, the last selection we push becomes the first processed.
        for (var i = operationDefinition.SelectionSet.Selections.Count - 1; i >= 0; i--)
        {
            var rootSelection = operationDefinition.SelectionSet.Selections[i];
            var rootSelectionSet = new SelectionSetNode([rootSelection]);
            indexBuilder.Register(selectionSetId, rootSelectionSet);

            var selectionSet = new SelectionSet(
                selectionSetId,
                rootSelectionSet,
                _schema.GetOperationType(operationDefinition.Operation),
                SelectionPath.Root);

            // firstSelectionSet gets overwritten each iteration and ends up holding
            // the selection from the last loop iteration (i=0), which corresponds to
            // the first mutation field in document order and the first to be processed.
            firstSelectionSet = selectionSet;
            backlog = backlog.PushWithLowerBound(OperationWorkItem.CreateRoot(selectionSet), ref backlogCostState);
        }

        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlogCostState);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = ISchemaDefinition.DefaultName,
            Options = _options,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            OperationStepCount = 0
        };

        return (node, firstSelectionSet);
    }

    private (PlanNode Node, SelectionSet First) CreateSubscriptionPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            _schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var workItem = OperationWorkItem.CreateRoot(selectionSet);
        var backlog = ImmutableStack<WorkItem>.Empty;
        var backlogCostState = BacklogCostState.Empty;
        backlog = backlog.PushWithLowerBound(workItem, ref backlogCostState);
        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                _options,
                currentMaxDepth: 0,
                ImmutableDictionary<int, int>.Empty,
                backlogCostState);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = "None",
            Options = _options,
            SelectionSetIndex = index,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            OperationStepCount = 0
        };

        return (node, selectionSet);
    }

    private PlanResult? Plan(
        string operationId,
        PriorityQueue<PlanNode, double> possiblePlans,
        bool emitPlannerEvents)
    {
        var eventSource = PlannerEventSource.Log;
        var searchSpace = (uint)possiblePlans.Count;
        var expandedNodes = 0;

        // TryBuildGreedyCompletePlan quickly builds one full plan by always choosing the currently
        // cheapest next option at each step.
        //
        // It gives the planner an initial best known complete cost, so the main search can skip branches
        // that are already worse. If it cannot finish a full plan, it returns null and the planner
        // continues without that early shortcut.
        var bestCompleteNode = TryBuildGreedyCompletePlan(possiblePlans);
        var bestCompleteCost = bestCompleteNode is null ? double.PositiveInfinity : GetCompleteCost(bestCompleteNode);

        while (possiblePlans.TryDequeue(out var current, out _))
        {
            expandedNodes++;
            var possiblePlansCount = possiblePlans.Count;
            searchSpace = MaxSearchSpace(Unsafe.As<int, uint>(ref possiblePlansCount), searchSpace);

            var backlog = current.Backlog;
            var backlogCostState = current.BacklogCostState;

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
            if (GetOptimisticLowerBound(current) >= bestCompleteCost)
            {
                continue;
            }

            if (backlog.IsEmpty)
            {
                // We found a complete plan. Keep it if it is cheaper than the current best plan.
                // If cost is the same, use a deterministic tie-break so results stay stable.
                var completeCost = GetCompleteCost(current);

                if (completeCost < bestCompleteCost
                    || (completeCost.Equals(bestCompleteCost)
                        && bestCompleteNode is not null
                        && ComparePlanDeterministically(current, bestCompleteNode) < 0))
                {
                    bestCompleteNode = current;
                    bestCompleteCost = completeCost;
                }

                continue;
            }

            // The backlog represents the tasks we have to complete to build out
            // the current possible plan. It's not guaranteed that this plan will work
            // out or that it is efficient.
            backlog = current.Backlog.PopWithLowerBound(out var workItem, ref backlogCostState);

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanRootSelections(wi, current, backlog, backlogCostState, possiblePlans);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, backlogCostState, possiblePlans);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        possiblePlans,
                        backlog,
                        backlogCostState);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        possiblePlans,
                        backlog,
                        backlogCostState);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, possiblePlans, backlog, backlogCostState);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, possiblePlans, backlog, backlogCostState);
                    break;

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }
        }

        if (bestCompleteNode is null)
        {
            return null;
        }

        return new PlanResult(
            bestCompleteNode.InternalOperationDefinition,
            bestCompleteNode.Steps,
            searchSpace,
            expandedNodes,
            bestCompleteNode.OperationStepCount);

        static uint MaxSearchSpace(uint val1, uint val2)
            => (val1 >= val2) ? val1 : val2;

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
    }

    private PlanNode? TryBuildGreedyCompletePlan(PriorityQueue<PlanNode, double> possiblePlans)
    {
        if (!possiblePlans.TryPeek(out var current, out _))
        {
            return null;
        }

        var candidates = new PriorityQueue<PlanNode, double>();

        while (true)
        {
            var backlog = current.Backlog;
            var backlogCostState = current.BacklogCostState;

            if (backlog.IsEmpty)
            {
                return current;
            }

            backlog = backlog.PopWithLowerBound(out var workItem, ref backlogCostState);

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanRootSelections(wi, current, backlog, backlogCostState, candidates);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, backlogCostState, candidates);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(
                        wi,
                        current,
                        candidates,
                        backlog,
                        backlogCostState);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(
                        wi,
                        wi.Lookup,
                        current,
                        candidates,
                        backlog,
                        backlogCostState);
                    break;

                case NodeFieldWorkItem wi:
                    PlanNode(wi, current, candidates, backlog, backlogCostState);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, candidates, backlog, backlogCostState);
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

    private static double GetOptimisticLowerBound(PlanNode node)
        => node.PathCost + node.BacklogLowerBound;

    private static double GetCompleteCost(PlanNode node)
        => node.PathCost;

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

    private static int ComparePlanDeterministically(PlanNode left, PlanNode right)
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
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState,
        PriorityQueue<PlanNode, double> possiblePlans)
        => PlanSelections(workItem, current, null, backlog, backlogCostState, possiblePlans);

    private void PlanLookupSelections(
        OperationWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState,
        PriorityQueue<PlanNode, double> possiblePlans)
    {
        current = InlineLookupRequirements(
            workItem.SelectionSet,
            current,
            lookup,
            workItem.EstimatedDepth,
            backlog,
            backlogCostState);
        PlanSelections(
            workItem,
            current,
            lookup,
            current.Backlog,
            current.BacklogCostState,
            possiblePlans);
    }

    private void PlanSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Lookup? lookup,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState,
        PriorityQueue<PlanNode, double> possiblePlans)
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

        backlog = backlog.PushWithLowerBound(unresolvable, current, stepDepth, ref backlogCostState);
        backlog = backlog.PushWithLowerBound(fieldsWithRequirements, stepId, stepDepth, ref backlogCostState);

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
        var backlogLowerBound = PlannerCostEstimator.EstimateBacklogLowerBound(
            current.Options,
            costState.MaxDepth,
            costState.OpsPerLevel,
            backlogCostState);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            Steps = current.Steps.Add(step),
            LastRequirementId = lastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private PlanNode InlineLookupRequirements(
        SelectionSet workItemSelectionSet,
        PlanNode current,
        Lookup lookup,
        int lookupStepDepth,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState)
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

                    backlog = backlog.PushWithLowerBound(
                        unresolvable,
                        current,
                        GetOperationStepDepth(current, step.Id),
                        ref backlogCostState);
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
            backlog = backlog.PushWithLowerBound(
                new OperationWorkItem(
                    OperationWorkItemKind.Lookup,
                    workItemSelectionSet with { Node = selectionSet },
                    FromSchema: lookup.SchemaName)
                {
                    Dependents = ImmutableHashSet<int>.Empty.Add(lookupStepId),
                    ParentDepth = lookupStepDepth
                },
                ref backlogCostState);
        }

        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                current.Options,
                current.MaxDepth,
                current.OpsPerLevel,
                backlogCostState);

        return current with
        {
            Steps = steps,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
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
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState)
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
                    ref backlogCostState,
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
        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                current.Options,
                current.MaxDepth,
                current.OpsPerLevel,
                backlogCostState);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            Steps = steps,
            LastRequirementId = requirementId,
            OperationStepCount = current.OperationStepCount,
            MaxDepth = current.MaxDepth,
            ExcessFanout = current.ExcessFanout,
            OpsPerLevel = current.OpsPerLevel,
            OperationStepDepths = current.OperationStepDepths
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanFieldWithRequirement(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState)
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
            backlogCostState);
        backlog = current.Backlog;
        backlogCostState = current.BacklogCostState;

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
                ref backlogCostState,
                ref steps);

        // if we have requirements that we could not inline into existing
        // nodes of the operation plan we will put it on the backlog to be
        // planned as another lookup.
        if (leftoverRequirements is not null)
        {
            indexBuilder.Register(
                workItem.Selection.SelectionSetId,
                leftoverRequirements);

            backlog = backlog.PushWithLowerBound(
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
                },
                ref backlogCostState);
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
                ref backlog,
                ref backlogCostState);

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
        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlogCostState);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            Steps = steps.Add(step),
            LastRequirementId = lastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanNodeLookup(
        NodeLookupWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState)
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

        backlog = backlog.PushWithLowerBound(unresolvable, current, stepDepth, ref backlogCostState);
        backlog = backlog.PushWithLowerBound(fieldsWithRequirements, stepId, stepDepth, ref backlogCostState);

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
        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlogCostState);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
            Steps = steps,
            LastRequirementId = current.LastRequirementId,
            OperationStepCount = current.OperationStepCount + 1,
            MaxDepth = costState.MaxDepth,
            ExcessFanout = costState.ExcessFanout,
            OpsPerLevel = costState.OpsPerLevel,
            OperationStepDepths = costState.StepDepths
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanNode(
        NodeFieldWorkItem workItem,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog,
        BacklogCostState backlogCostState)
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

            backlog = backlog.PushWithLowerBound(newWorkItem, ref backlogCostState);
        }

        var costState = AddOperationStepCostState(current, fallbackQueryStepId, stepDepth);
        var backlogLowerBound =
            PlannerCostEstimator.EstimateBacklogLowerBound(
                current.Options,
                costState.MaxDepth,
                costState.OpsPerLevel,
                backlogCostState);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            Options = current.Options,
            SelectionSetIndex = index,
            Backlog = backlog,
            BacklogCostState = backlogCostState,
            BacklogLowerBound = backlogLowerBound,
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

        possiblePlans.Enqueue(next, _schema);
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
        ref ImmutableStack<WorkItem> backlog,
        ref BacklogCostState backlogCostState)
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
        backlog = backlog.PushWithLowerBound(unresolvable, current, stepDepth, ref backlogCostState);
        backlog = backlog.PushWithLowerBound(fieldsWithRequirements, stepId, stepDepth, ref backlogCostState);
        return resolvable;
    }

    private SelectionSetNode? TryInlineFieldRequirements(
        FieldRequirementWorkItem workItem,
        ref PlanNode current,
        OperationPlanStep currentStep,
        SelectionSetIndexBuilder index,
        ref ImmutableStack<WorkItem> backlog,
        ref BacklogCostState backlogCostState,
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
                    backlog = backlog.PushWithLowerBound(
                        new OperationWorkItem(
                            OperationWorkItemKind.Lookup,
                            selectionSet,
                            FromSchema: current.SchemaName)
                        {
                            ParentDepth = GetOperationStepDepth(current, step.Id)
                        },
                        ref backlogCostState);
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
        uint SearchSpace,
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

file static class Extensions
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

    private static ImmutableHashSet<string> GetCandidateSchemas(
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

    public static ImmutableStack<WorkItem> PopWithLowerBound(
        this ImmutableStack<WorkItem> backlog,
        out WorkItem workItem,
        ref BacklogCostState backlogCostState)
    {
        // BacklogCostState is the authoritative incremental h(n) state.
        // Every pop/push must update it in lockstep with the stack so
        // branch scoring stays O(1) per transition.
        backlog = backlog.Pop(out workItem);
        backlogCostState = PlannerCostEstimator.RemoveWorkItemLowerBound(backlogCostState, workItem);

        return backlog;
    }

    public static ImmutableStack<WorkItem> PushWithLowerBound(
        this ImmutableStack<WorkItem> backlog,
        WorkItem workItem,
        ref BacklogCostState backlogCostState)
    {
        backlogCostState = PlannerCostEstimator.AddWorkItemLowerBound(backlogCostState, workItem);
        return backlog.Push(workItem);
    }

    public static ImmutableStack<WorkItem> PushWithLowerBound(
        this ImmutableStack<WorkItem> backlog,
        ImmutableStack<SelectionSet> unresolvable,
        PlanNode current,
        int parentDepth,
        ref BacklogCostState backlogCostState)
    {
        if (unresolvable.IsEmpty)
        {
            return backlog;
        }

        foreach (var selectionSet in unresolvable.Reverse())
        {
            var workItem = new OperationWorkItem(
                selectionSet.Path.IsRoot
                    ? OperationWorkItemKind.Root
                    : OperationWorkItemKind.Lookup,
                selectionSet,
                FromSchema: current.SchemaName)
            {
                ParentDepth = parentDepth
            };
            backlog = backlog.PushWithLowerBound(workItem, ref backlogCostState);
        }

        return backlog;
    }

    /// <summary>
    /// Pushes the field requirements to the backlog.
    /// </summary>
    /// <param name="backlog">
    /// The backlog.
    /// </param>
    /// <param name="fieldsWithRequirements">
    /// The field requirements.
    /// </param>
    /// <param name="stepId">
    /// The step that the fields with requirements were intended to be included in.
    /// </param>
    /// <param name="parentDepth">
    /// The depth of the step that produced the requirement work items.
    /// </param>
    /// <param name="backlogCostState">
    /// The current optimistic lower-bound state for the backlog.
    /// </param>
    /// <returns>
    /// The updated backlog.
    /// </returns>
    public static ImmutableStack<WorkItem> PushWithLowerBound(
        this ImmutableStack<WorkItem> backlog,
        ImmutableStack<FieldSelection> fieldsWithRequirements,
        int stepId,
        int parentDepth,
        ref BacklogCostState backlogCostState)
    {
        if (fieldsWithRequirements.IsEmpty)
        {
            return backlog;
        }

        foreach (var selection in fieldsWithRequirements.Reverse())
        {
            var workItem = new FieldRequirementWorkItem(selection, stepId)
            {
                ParentDepth = parentDepth
            };
            backlog = backlog.PushWithLowerBound(workItem, ref backlogCostState);
        }

        return backlog;
    }

    public static void Enqueue(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        FusionSchemaDefinition compositeSchema)
    {
        var nextWorkItem =
            planNodeTemplate.Backlog.IsEmpty
                ? null
                : planNodeTemplate.Backlog.Peek();

        // we reset the resolution cost so that the next plan is not chosen based
        // on the last resolutions cost.
        planNodeTemplate = planNodeTemplate with { ResolutionCost = 0 };

        switch (nextWorkItem)
        {
            case null:
            case NodeFieldWorkItem:
                possiblePlans.EnqueueWithCost(planNodeTemplate, compositeSchema);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                possiblePlans.EnqueueRootPlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Lookup } wi:
                possiblePlans.EnqueueLookupPlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            case FieldRequirementWorkItem wi:
                possiblePlans.EnqueueRequirePlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            case NodeLookupWorkItem { Lookup: null } wi:
                possiblePlans.EnqueueNodeLookupPlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            default:
                throw new NotSupportedException(
                    "The work item type is not supported.");
        }
    }

    public static void EnqueueWithCost(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode node,
        FusionSchemaDefinition compositeSchema)
        => possiblePlans.Enqueue(node, PlannerCostEstimator.EstimateTotalCost(node, compositeSchema));

    private static void EnqueueRootPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        foreach (var (schemaName, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            possiblePlans.EnqueueWithCost(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    ResolutionCost = resolutionCost
                },
                compositeSchema);
        }
    }

    private static void EnqueueLookupPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlogCostState = planNodeTemplate.BacklogCostState;
        var backlog = planNodeTemplate.Backlog.PopWithLowerBound(out _, ref backlogCostState);
        var allCandidateSchemas = planNodeTemplate.GetCandidateSchemas(workItem.SelectionSet.Id);
        var type = (FusionComplexTypeDefinition)workItem.SelectionSet.Type;

        double EstimateBranchLowerBound(BacklogCostState branchBacklogCostState)
            => PlannerCostEstimator.EstimateBacklogLowerBound(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklogCostState);

        // Each branch starts from the same popped template and
        // mutates a local copy of backlog state.
        // This avoids recomputing backlog shape
        // from collections for every candidate.
        foreach (var (toSchema, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            if (toSchema.Equals(workItem.FromSchema, StringComparison.Ordinal))
            {
                continue;
            }

            if (compositeSchema.TryGetBestDirectLookup(
                type,
                allCandidateSchemas.Remove(toSchema),
                toSchema,
                out var bestLookup))
            {
                var lookupWorkItem = workItem with { Lookup = bestLookup };
                var branchBacklogCostState = backlogCostState;
                var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                possiblePlans.EnqueueWithCost(
                    planNodeTemplate with
                    {
                        SchemaName = toSchema,
                        ResolutionCost = resolutionCost,
                        Backlog = branchBacklog,
                        BacklogCostState = branchBacklogCostState,
                        BacklogLowerBound = branchBacklogLowerBound
                    },
                    compositeSchema);
                continue;
            }

            var hasEnqueuedDirectLookup = false;
            foreach (var lookup in compositeSchema.GetPossibleLookupsOrdered(workItem.SelectionSet.Type, toSchema))
            {
                var lookupWorkItem = workItem with { Lookup = lookup };
                var branchBacklogCostState = backlogCostState;
                var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                possiblePlans.EnqueueWithCost(
                    planNodeTemplate with
                    {
                        SchemaName = toSchema,
                        ResolutionCost = resolutionCost,
                        Backlog = branchBacklog,
                        BacklogCostState = branchBacklogCostState,
                        BacklogLowerBound = branchBacklogLowerBound
                    },
                    compositeSchema);

                hasEnqueuedDirectLookup = true;
            }

            // If we did not find a direct lookup for the type of the current selection set,
            // we attempt to walk up the path we came from to see if we can lookup a parent
            // type or if we can just reuse the entire path we came from, e.g. viewer { ... }.
            if (!hasEnqueuedDirectLookup)
            {
                foreach (var (lookupThroughPathWorkItem, cost, index) in GetPossibleLookupsThroughPath(
                    planNodeTemplate,
                    workItem,
                    toSchema,
                    compositeSchema).OrderBy(
                    t => LookupOrderingKey(t.WorkItem.Lookup),
                    StringComparer.Ordinal))
                {
                    var branchBacklogCostState = backlogCostState;
                    var branchBacklog = backlog.PushWithLowerBound(
                        lookupThroughPathWorkItem,
                        ref branchBacklogCostState);
                    var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                    possiblePlans.EnqueueWithCost(
                        planNodeTemplate with
                        {
                            SchemaName = toSchema,
                            SelectionSetIndex = index,
                            ResolutionCost = resolutionCost + cost,
                            Backlog = branchBacklog,
                            BacklogCostState = branchBacklogCostState,
                            BacklogLowerBound = branchBacklogLowerBound
                        },
                        compositeSchema);
                }
            }
        }
    }

    private static void EnqueueNodeLookupPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        NodeLookupWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlogCostState = planNodeTemplate.BacklogCostState;
        var backlog = planNodeTemplate.Backlog.PopWithLowerBound(out _, ref backlogCostState);
        var type = workItem.SelectionSet.Type;
        var hasEnqueuedLookup = false;

        double EstimateBranchLowerBound(BacklogCostState branchBacklogCostState)
            => PlannerCostEstimator.EstimateBacklogLowerBound(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklogCostState);

        // Same branching rule as lookup work items:
        // copy backlog state per branch, then
        // materialize a new node with the
        // branch-local lower bound.
        foreach (var (schemaName, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            // If we have multiple id lookups in a single schema,
            // we try to choose one that returns the desired type directly
            // and not an abstract type.
            var byIdLookup = compositeSchema
                .GetPossibleLookupsOrdered(type, schemaName)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }] && !l.IsInternal);

            if (byIdLookup is null)
            {
                continue;
            }

            var lookupWorkItem = workItem with { Lookup = byIdLookup };
            var branchBacklogCostState = backlogCostState;
            var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
            var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
            possiblePlans.EnqueueWithCost(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    ResolutionCost = resolutionCost,
                    Backlog = branchBacklog,
                    BacklogCostState = branchBacklogCostState,
                    BacklogLowerBound = branchBacklogLowerBound
                },
                compositeSchema);

            hasEnqueuedLookup = true;
        }

        // It could be that we didn't find a suitable source schema for the requested selections
        // that also has a by id resolver.
        // In this case we enqueue the best matching by id lookup of any source schema.
        if (!hasEnqueuedLookup)
        {
            var byIdLookup = compositeSchema
                .GetPossibleLookupsOrdered(type)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }] && !l.IsInternal)
                    ?? throw new InvalidOperationException(
                        $"Expected to have at least one lookup with just an 'id' argument for type '{type.Name}'.");

            var lookupWorkItem = workItem with { Lookup = byIdLookup };
            var branchBacklogCostState = backlogCostState;
            var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
            var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
            possiblePlans.EnqueueWithCost(
                planNodeTemplate with
                {
                    SchemaName = byIdLookup.SchemaName,
                    Backlog = branchBacklog,
                    BacklogCostState = branchBacklogCostState,
                    BacklogLowerBound = branchBacklogLowerBound
                },
                compositeSchema);
        }
    }

    private static void EnqueueRequirePlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        FieldRequirementWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlogCostState = planNodeTemplate.BacklogCostState;
        var backlog = planNodeTemplate.Backlog.PopWithLowerBound(out _, ref backlogCostState);
        var allCandidateSchemas = planNodeTemplate.GetCandidateSchemas(workItem.Selection.SelectionSetId);
        var selectionSetType = workItem.Selection.Field.DeclaringType;

        double EstimateBranchLowerBound(BacklogCostState branchBacklogCostState)
            => PlannerCostEstimator.EstimateBacklogLowerBound(
                planNodeTemplate.Options,
                planNodeTemplate.MaxDepth,
                planNodeTemplate.OpsPerLevel,
                branchBacklogCostState);

        // Requirement planning can fork into inline and lookup paths.
        // Both are scored from the same popped template by cloning and
        // mutating backlog state per candidate.
        var requirementSchemas =
            compositeSchema.TryGetFieldResolution(selectionSetType, workItem.Selection.Field.Name, out var fieldResolution)
                ? fieldResolution.Schemas
                : workItem.Selection.Field.Sources.Schemas.OrderBy(static t => t, StringComparer.Ordinal).ToImmutableArray();

        foreach (var schemaName in requirementSchemas)
        {
            var candidateSchemas = allCandidateSchemas.Remove(schemaName);

            if (schemaName == planNodeTemplate.SchemaName)
            {
                var inlineWorkItem = workItem;
                var inlineBacklogCostState = backlogCostState;
                var inlineBacklog = backlog.PushWithLowerBound(inlineWorkItem, ref inlineBacklogCostState);
                var inlineBacklogLowerBound = EstimateBranchLowerBound(inlineBacklogCostState);
                possiblePlans.EnqueueWithCost(
                    planNodeTemplate with
                    {
                        Backlog = inlineBacklog,
                        BacklogCostState = inlineBacklogCostState,
                        BacklogLowerBound = inlineBacklogLowerBound,
                    },
                    compositeSchema);

                if (compositeSchema.TryGetBestDirectLookup(
                    selectionSetType,
                    candidateSchemas.Remove(schemaName),
                    schemaName,
                    out var bestLookup))
                {
                    var lookupWorkItem = workItem with { Lookup = bestLookup };
                    var branchBacklogCostState = backlogCostState;
                    var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                    var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                    possiblePlans.EnqueueWithCost(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            BacklogCostState = branchBacklogCostState,
                            BacklogLowerBound = branchBacklogLowerBound
                        },
                        compositeSchema);
                    continue;
                }

                foreach (var lookup in compositeSchema.GetPossibleLookupsOrdered(selectionSetType, schemaName))
                {
                    var lookupWorkItem = workItem with { Lookup = lookup };
                    var branchBacklogCostState = backlogCostState;
                    var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                    var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                    possiblePlans.EnqueueWithCost(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            BacklogCostState = branchBacklogCostState,
                            BacklogLowerBound = branchBacklogLowerBound
                        },
                        compositeSchema);
                }
            }
            else
            {
                if (compositeSchema.TryGetBestDirectLookup(
                    selectionSetType,
                    candidateSchemas,
                    schemaName,
                    out var bestLookup))
                {
                    var lookupWorkItem = workItem with { Lookup = bestLookup };
                    var branchBacklogCostState = backlogCostState;
                    var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                    var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                    possiblePlans.EnqueueWithCost(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            BacklogCostState = branchBacklogCostState,
                            BacklogLowerBound = branchBacklogLowerBound
                        },
                        compositeSchema);
                    continue;
                }

                foreach (var lookup in compositeSchema.GetPossibleLookupsOrdered(selectionSetType, schemaName))
                {
                    var lookupWorkItem = workItem with { Lookup = lookup };
                    var branchBacklogCostState = backlogCostState;
                    var branchBacklog = backlog.PushWithLowerBound(lookupWorkItem, ref branchBacklogCostState);
                    var branchBacklogLowerBound = EstimateBranchLowerBound(branchBacklogCostState);
                    possiblePlans.EnqueueWithCost(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            Backlog = branchBacklog,
                            BacklogCostState = branchBacklogCostState,
                            BacklogLowerBound = branchBacklogLowerBound
                        },
                        compositeSchema);
                }
            }
        }
    }

    public static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this FusionSchemaDefinition compositeSchema,
        SelectionSet selectionSet)
    {
        ArgumentNullException.ThrowIfNull(compositeSchema);
        ArgumentNullException.ThrowIfNull(selectionSet);

        var candidateSchemas = new HashSet<string>(StringComparer.Ordinal);
        var rankedSchemas = new List<(string SchemaName, double Cost)>();

        CollectCandidateSchemas(
            compositeSchema,
            selectionSet.Type,
            selectionSet.Selections,
            candidateSchemas);

        foreach (var schemaName in candidateSchemas)
        {
            var fit = new SchemaFit();

            AnalyzeSchemaFit(
                compositeSchema,
                selectionSet.Type,
                selectionSet.Selections,
                schemaName,
                fit);

            rankedSchemas.Add((schemaName, fit.ComputeCost()));
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

                        if (compositeSchema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                        {
                            foreach (var schemaName in fieldResolution.Schemas)
                            {
                                candidateSchemas.Add(schemaName);
                            }
                        }
                        else
                        {
                            foreach (var schemaName in field.Sources.Schemas)
                            {
                                candidateSchemas.Add(schemaName);
                            }
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

        static void AnalyzeSchemaFit(
            FusionSchemaDefinition compositeSchema,
            ITypeDefinition type,
            IReadOnlyList<ISelectionNode> selections,
            string schemaName,
            SchemaFit fit)
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

                        fit.TotalFields++;

                        if (compositeSchema.TryGetFieldResolution(complexType, field.Name, out var fieldResolution))
                        {
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
                                fit.Unresolvable++;

                                foreach (var spilloverSchema in fieldResolution.Schemas)
                                {
                                    fit.SpilloverSchemas.Add(spilloverSchema);
                                }
                            }
                        }
                        else
                        {
                            if (field.Sources.ContainsSchema(schemaName))
                            {
                                fit.Resolvable++;

                                if (field.Sources.TryGetMember(schemaName, out var source)
                                    && source.Requirements is not null)
                                {
                                    fit.WithRequirements++;
                                }
                            }
                            else
                            {
                                fit.Unresolvable++;

                                foreach (var spilloverSchema in field.Sources.Schemas)
                                {
                                    if (!spilloverSchema.Equals(schemaName, StringComparison.Ordinal))
                                    {
                                        fit.SpilloverSchemas.Add(spilloverSchema);
                                    }
                                }
                            }
                        }

                        if (fieldNode.SelectionSet is not null)
                        {
                            AnalyzeSchemaFit(
                                compositeSchema,
                                field.Type.AsTypeDefinition(),
                                fieldNode.SelectionSet.Selections,
                                schemaName,
                                fit);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        var typeCondition = type;

                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeCondition = compositeSchema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                        }

                        AnalyzeSchemaFit(
                            compositeSchema,
                            typeCondition,
                            inlineFragmentNode.SelectionSet.Selections,
                            schemaName,
                            fit);
                        break;
                }
            }
        }
    }

    private sealed class SchemaFit
    {
        public int Resolvable { get; set; }

        public int Unresolvable { get; set; }

        public int WithRequirements { get; set; }

        public int TotalFields { get; set; }

        public HashSet<string> SpilloverSchemas { get; } = new(StringComparer.Ordinal);

        public double ComputeCost()
        {
            if (TotalFields == 0)
            {
                return 0.0;
            }

            var coverageRatio = (double)Resolvable / TotalFields;
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
    private static IEnumerable<(OperationWorkItem WorkItem, double Cost, ISelectionSetIndex SelectionSetIndex)>
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
                if (!fieldPathItem.Field.Sources.ContainsSchema(schemaName))
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

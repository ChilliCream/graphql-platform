using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Planning.Partitioners;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
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

    public OperationPlanner(
        FusionSchemaDefinition schema,
        OperationCompiler operationCompiler)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(operationCompiler);

        _schema = schema;
        _operationCompiler = operationCompiler;
        _mergeRewriter = new MergeSelectionSetRewriter(schema);
        _partitioner = new SelectionSetPartitioner(schema);
        _selectionSetByTypePartitioner = new SelectionSetByTypePartitioner(schema);
        _nodeFieldSelectionSetPartitioner = new NodeFieldSelectionSetPartitioner(schema);
    }

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
                possiblePlans.Enqueue(
                    node with
                    {
                        SchemaName = schemaName,
                        BacklogCost = node.Backlog.Count() + resolutionCost
                    });
            }

            if (possiblePlans.Count < 1)
            {
                possiblePlans.Enqueue(
                    node with
                    {
                        BacklogCost = node.Backlog.Count()
                    });
            }

            var plan = Plan(possiblePlans);

            if (!plan.HasValue)
            {
                throw new InvalidOperationException("No possible plan was found.");
            }

            internalOperationDefinition = plan.Value.InternalOperationDefinition;
            planSteps = plan.Value.Steps;

            internalOperationDefinition = AddTypeNameToAbstractSelections(
                internalOperationDefinition,
                _schema.GetOperationType(operationDefinition.Operation));
        }

        var operation = _operationCompiler.Compile(id, hash, internalOperationDefinition);

        return BuildExecutionPlan(
            operation,
            operationDefinition,
            planSteps);
    }

    private (PlanNode Node, SelectionSet First) CreateQueryPlanBase(
        OperationDefinitionNode operationDefinition,
        string shortHash,
        ISelectionSetIndex index)
    {
        var indexBuilder = index.ToBuilder();
        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            _schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var input = new RootSelectionSetPartitionerInput { SelectionSet = selectionSet, SelectionSetIndex = index };
        var result = _nodeFieldSelectionSetPartitioner.Partition(input);

        var backlog = ImmutableStack<WorkItem>.Empty;

        if (result.SelectionSet is not null)
        {
            var workItem = OperationWorkItem.CreateRoot(result.SelectionSet);
            backlog = backlog.Push(workItem);
        }

        if (result.NodeFields is not null)
        {
            foreach (var nodeField in result.NodeFields)
            {
                var nodeWorkItem = new NodeWorkItem(nodeField);
                backlog = backlog.Push(nodeWorkItem);
            }
        }

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = "None",
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            PathCost = 1,
            BacklogCost = backlog.Count()
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
            backlog = backlog.Push(OperationWorkItem.CreateRoot(selectionSet));
        }

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = ISchemaDefinition.DefaultName,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            PathCost = 1,
            BacklogCost = 1
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

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            InternalOperationDefinition = operationDefinition,
            ShortHash = shortHash,
            SchemaName = "None",
            SelectionSetIndex = index,
            Backlog = ImmutableStack<WorkItem>.Empty.Push(workItem),
            PathCost = 1,
            BacklogCost = 1
        };

        return (node, selectionSet);
    }

    private (OperationDefinitionNode InternalOperationDefinition, ImmutableList<PlanStep> Steps)? Plan(
        PriorityQueue<PlanNode, double> possiblePlans)
    {
        while (possiblePlans.TryDequeue(out var current, out _))
        {
            var backlog = current.Backlog;

            if (backlog.IsEmpty)
            {
                // If the backlog is empty, the planning process is complete, and we can return the
                // steps to build the actual execution plan.
                return (current.InternalOperationDefinition, current.Steps);
            }

            // The backlog represents the tasks we have to complete to build out
            // the current possible plan. It's not guaranteed that this plan will work
            // out or that it is efficient.
            backlog = current.Backlog.Pop(out var workItem);

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanRootSelections(wi, current, backlog, possiblePlans);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    PlanLookupSelections(wi, lookup, current, backlog, possiblePlans);
                    break;

                case FieldRequirementWorkItem { Lookup: null } wi:
                    PlanInlineFieldWithRequirements(wi, current, possiblePlans, backlog);
                    break;

                case FieldRequirementWorkItem wi:
                    PlanFieldWithRequirement(wi, wi.Lookup, current, possiblePlans, backlog);
                    break;

                case NodeWorkItem wi:
                    PlanNode(wi, current, possiblePlans, backlog);
                    break;

                case NodeLookupWorkItem { Lookup: { } lookup } wi:
                    PlanNodeLookup(wi, lookup, current, possiblePlans, backlog);
                    break;

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }
        }

        return null;
    }

    private void PlanRootSelections(
        OperationWorkItem workItem,
        PlanNode current,
        ImmutableStack<WorkItem> backlog,
        PriorityQueue<PlanNode, double> possiblePlans)
        => PlanSelections(workItem, current, null, backlog, possiblePlans);

    private void PlanLookupSelections(
        OperationWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        ImmutableStack<WorkItem> backlog,
        PriorityQueue<PlanNode, double> possiblePlans)
    {
        current = InlineLookupRequirements(workItem.SelectionSet, current, lookup, backlog);
        PlanSelections(workItem, current, lookup, current.Backlog, possiblePlans);
    }

    private void PlanSelections(
        OperationWorkItem workItem,
        PlanNode current,
        Lookup? lookup,
        ImmutableStack<WorkItem> backlog,
        PriorityQueue<PlanNode, double> possiblePlans)
    {
        var stepId = current.Steps.NextId();
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

        backlog = backlog.Push(unresolvable);
        backlog = backlog.Push(fieldsWithRequirements, stepId);

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
            Source = source
        };

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index,
            Backlog = backlog,
            Steps = current.Steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = lastRequirementId
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private PlanNode InlineLookupRequirements(
        SelectionSet workItemSelectionSet,
        PlanNode current,
        Lookup lookup,
        ImmutableStack<WorkItem> backlog)
    {
        var processed = new HashSet<string>();
        var lookupStepId = current.Steps.NextId();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.Requirements;

        index.Register(workItemSelectionSet.Id, selectionSet);

        var internalOperation = InlineSelections(
            current.InternalOperationDefinition,
            index,
            workItemSelectionSet.Type,
            workItemSelectionSet.Id,
            selectionSet,
            inlineInternal: true);

        foreach (var (step, stepIndex, schemaName) in current.GetCandidateSteps(workItemSelectionSet.Id))
        {
            if (!processed.Add(schemaName))
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

                    backlog = backlog.Push(unresolvable);
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
                    workItemSelectionSet with { Node = selectionSet })
                {
                    Dependents = ImmutableHashSet<int>.Empty.Add(lookupStepId)
                });
        }

        return current with
        {
            Steps = steps,
            Backlog = backlog,
            SelectionSetIndex = index,
            InternalOperationDefinition = internalOperation
        };
    }

    private void PlanInlineFieldWithRequirements(
        FieldRequirementWorkItem workItem,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog)
    {
        if (current.Steps.ById(workItem.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var requirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{requirementId}";

        var success =
            TryInlineFieldRequirements(
                    workItem,
                    ref current,
                    currentStep,
                    requirementKey,
                    index,
                    ref backlog,
                    ref steps)
                is null;

        if (!success)
        {
            return;
        }

        var field = workItem.Selection.Field;
        var fieldSource = field.Sources[current.SchemaName];
        var arguments = new List<ArgumentNode>(workItem.Selection.Node.Arguments);

        foreach (var argument in fieldSource.Requirements!.Arguments)
        {
            // arguments that are exposed on the composite schema
            // are not requirements, and we can skip them.
            if (field.Arguments.ContainsName(argument.Name))
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

        for (var i = 0; i < fieldSource.Requirements.Arguments.Length; i++)
        {
            var argument = fieldSource.Requirements.Arguments[i];
            var fieldSelectionMap = fieldSource.Requirements.Fields[i];

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

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index,
            Backlog = backlog,
            Steps = steps,
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = requirementId
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanFieldWithRequirement(
        FieldRequirementWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog)
    {
        var selectionSetStub = new SelectionSet(
            workItem.Selection.SelectionSetId,
            new SelectionSetNode([workItem.Selection.Node]),
            workItem.Selection.Field.DeclaringType,
            workItem.Selection.Path);
        current = InlineLookupRequirements(selectionSetStub, current, lookup, backlog);

        if (current.Steps.ById(workItem.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        var steps = current.Steps;
        var stepId = current.Steps.NextId();
        var indexBuilder = current.SelectionSetIndex.ToBuilder();
        var lastRequirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{lastRequirementId}";
        var requirements = ImmutableDictionary<string, OperationRequirement>.Empty;

        var leftoverRequirements =
            TryInlineFieldRequirements(
                workItem,
                ref current,
                currentStep,
                requirementKey,
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
                    RequirementKey: requirementKey)
                {
                    Dependents = ImmutableHashSet<int>.Empty.Add(stepId)
                });
        }

        var field = workItem.Selection.Field;
        var fieldSource = field.Sources[current.SchemaName];
        var arguments = new List<ArgumentNode>(workItem.Selection.Node.Arguments);

        for (var i = 0; i < fieldSource.Requirements!.Arguments.Length; i++)
        {
            var argument = fieldSource.Requirements.Arguments[i];
            var fieldSelectionMap = fieldSource.Requirements.Fields[i];

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

        foreach (var argument in fieldSource.Requirements!.Arguments)
        {
            // arguments that are exposed on the composite schema
            // are not requirements, and we can skip them.
            if (field.Arguments.ContainsName(argument.Name))
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
            Type = field.DeclaringType,
            SchemaName = current.SchemaName,
            RootSelectionSetId = index.GetId(selectionSetNode),
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, indexBuilder),
            Requirements = requirements,
            Target = workItem.Selection.Path,
            Source = source
        };

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            SelectionSetIndex = indexBuilder,
            Backlog = backlog,
            Steps = steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = lastRequirementId
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanNodeLookup(
        NodeLookupWorkItem workItem,
        Lookup lookup,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog)
    {
        var stepId = current.Steps.NextId();
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

        backlog = backlog.Push(unresolvable);
        backlog = backlog.Push(fieldsWithRequirements, stepId);

        var selectionSetNode = resolvable
            .WithSelections([
                new FieldNode(IntrospectionFieldNames.TypeName),
                ..resolvable.Selections
            ]);

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
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
            Target = SelectionPath.Root,
            Source = SelectionPath.Root
        };

        var nodePlanStep = current.Steps.OfType<NodePlanStep>().LastOrDefault()
            ?? throw new InvalidOperationException($"Expected to find a {nameof(NodePlanStep)} in the existing steps.");

        var steps = current.Steps;

        // Add a new branch to the existing node plan step
        steps = steps.Replace(nodePlanStep, nodePlanStep with
        {
            Branches = nodePlanStep.Branches.SetItem(workItem.SelectionSet.Type.Name, operationPlanStep)
        });

        // Add the lookup operation to the steps
        steps = steps.Add(operationPlanStep);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index,
            Backlog = backlog,
            Steps = steps,
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = current.LastRequirementId
        };

        possiblePlans.Enqueue(next, _schema);
    }

    private void PlanNode(
        NodeWorkItem workItem,
        PlanNode current,
        PriorityQueue<PlanNode, double> possiblePlans,
        ImmutableStack<WorkItem> backlog)
    {
        var stepId = current.Steps.NextId();
        var fallbackQueryStepId = stepId + 1;
        var index = current.SelectionSetIndex;
        var nodeField = workItem.NodeField;
        var responseName = nodeField.Alias?.Value ?? nodeField.Name.Value;
        var selectionPath = SelectionPath.Root.AppendField(responseName);

        var idArgumentValue = nodeField.Arguments.First(a => a.Name.Value == "id").Value;

        var selectionSet = new SelectionSet(
            index.GetId(nodeField.SelectionSet!),
            nodeField.SelectionSet!,
            _schema.Types["Node"],
            selectionPath);

        var input = new SelectionSetByTypePartitionerInput
        {
            SelectionSet = selectionSet,
            SelectionSetIndex = index
        };

        (var sharedSelectionSet, var selectionSetsByType, index) = _selectionSetByTypePartitioner.Partition(input);

        var nodeFieldSelectionSet =
            new SelectionSetNode([
                new FieldNode(IntrospectionFieldNames.TypeName),
                ..sharedSelectionSet?.Selections ?? []
            ]);
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
            Requirements = ImmutableDictionary<string, OperationRequirement>.Empty,
            Target = SelectionPath.Root,
            Source = SelectionPath.Root
        };

        var nodeStep = new NodePlanStep
        {
            Id = stepId,
            FallbackQuery = fallbackQueryStep,
            ResponseName = responseName,
            IdValue = idArgumentValue
        };

        foreach (var (type, selectionSetNode) in selectionSetsByType)
        {
            var nodeSelectionSet = new SelectionSet(
                index.GetId(selectionSetNode),
                selectionSetNode,
                type,
                selectionPath);

            var newWorkItem = new NodeLookupWorkItem(
                Lookup: null,
                responseName,
                idArgumentValue,
                nodeSelectionSet);

            backlog = backlog.Push(newWorkItem);
        }

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            InternalOperationDefinition = current.InternalOperationDefinition,
            ShortHash = current.ShortHash,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index,
            Backlog = backlog,
            Steps = current.Steps
                .Add(nodeStep)
                .Add(fallbackQueryStep),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = current.LastRequirementId
        };

        possiblePlans.Enqueue(next, _schema);
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
        FieldSelection selection,
        PlanNode current,
        SelectionSetIndexBuilder index,
        ref ImmutableStack<WorkItem> backlog)
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
        backlog = backlog.Push(unresolvable);
        backlog = backlog.Push(fieldsWithRequirements, stepId);
        return resolvable;
    }

    private SelectionSetNode? TryInlineFieldRequirements(
        FieldRequirementWorkItem workItem,
        ref PlanNode current,
        OperationPlanStep currentStep,
        string requirementKey,
        SelectionSetIndexBuilder index,
        ref ImmutableStack<WorkItem> backlog,
        ref ImmutableList<PlanStep> steps)
    {
        var field = workItem.Selection.Field;
        var fieldSource = field.Sources[current.SchemaName];

        // TODO: we need a deep copy of this selection set or there might be problems if a requirement
        // is used on different parts of the operation.
        var requirements = fieldSource.Requirements!.Requirements;

        index.Register(
            workItem.Selection.SelectionSetId,
            requirements);

        var internalOperation =
            InlineSelections(
                current.InternalOperationDefinition,
                index,
                workItem.Selection.Field.DeclaringType,
                workItem.Selection.SelectionSetId,
                requirements,
                inlineInternal: true);
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
                            RequirementKey: requirementKey));
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

    private OperationDefinitionNode InlineSelections(
        OperationDefinitionNode operation,
        SelectionSetIndexBuilder index,
        ITypeDefinition selectionSetType,
        uint targetSelectionSetId,
        SelectionSetNode selectionsToInline,
        bool inlineInternal = false)
    {
        List<SelectionSetNode>? backlog = null;

        var rewriter = SyntaxRewriter.Create<List<ISyntaxNode>>(
            rewrite: (node, path) =>
            {
                if (node is not SelectionSetNode selectionSet)
                {
                    return node;
                }

                if (path.Count > 1 && path[^1] is InlineFragmentNode)
                {
                    Debugger.Break();
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

                index.Register(originalSelectionSet, newSelectionSet);
                return newSelectionSet;
            },
            enter: (node, path) =>
            {
                path.Push(node);
                return path;
            },
            leave: (_, path) =>
            {
                if (false)
                {
                    //
                }

                path.Pop();
            });

        return (OperationDefinitionNode)rewriter.Rewrite(operation, [])!;

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
                if (!index.IsRegistered(selectionSet))
                {
                    index.Register(selectionSet);
                }

                foreach (var selection in selectionSet.Selections)
                {
                    switch (selection)
                    {
                        case FieldNode  { SelectionSet: { } fieldSelectionSet }:
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
                if (node is not FieldNode fieldNode || fieldNode.SelectionSet is null)
                {
                    return node;
                }

                var type = path.Peek();

                if (type.IsAbstractType())
                {
                    // we add the __typename field to all seelection sets that have
                    // an abstract type context as we need the type context for
                    // runtime decisions.
                    //
                    // The __typename field is marked as a requirement to differentiate between a user
                    // required __typename and a runtime required type information.
                    var typenameNode = new FieldNode(IntrospectionFieldNames.TypeName)
                        .WithDirectives([new DirectiveNode("fusion__requirement")]);
                    return fieldNode.WithSelectionSet(new SelectionSetNode([
                        typenameNode, ..fieldNode.SelectionSet.Selections
                    ]));
                }

                return node;
            },
            (node, path) =>
            {
                if (node is FieldNode { SelectionSet: not null } fieldNode
                    && path.Peek() is IComplexTypeDefinition complexType)
                {
                    var field = complexType.Fields[fieldNode.Name.Value];

                    path.Push(field.Type.AsTypeDefinition());
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

    public static ImmutableStack<WorkItem> Push(
        this ImmutableStack<WorkItem> backlog,
        ImmutableStack<SelectionSet> unresolvable)
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
                selectionSet);
            backlog = backlog.Push(workItem);
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
    /// <returns>
    /// The updated backlog.
    /// </returns>
    public static ImmutableStack<WorkItem> Push(
        this ImmutableStack<WorkItem> backlog,
        ImmutableStack<FieldSelection> fieldsWithRequirements,
        int stepId)
    {
        if (fieldsWithRequirements.IsEmpty)
        {
            return backlog;
        }

        foreach (var selection in fieldsWithRequirements.Reverse())
        {
            var workItem = new FieldRequirementWorkItem(selection, stepId);
            backlog = backlog.Push(workItem);
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

        switch (nextWorkItem)
        {
            case null:
            case NodeWorkItem:
                possiblePlans.Enqueue(planNodeTemplate);
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

    private static void EnqueueRootPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        foreach (var (schemaName, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            possiblePlans.Enqueue(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    PathCost = planNodeTemplate.PathCost + 1,
                    BacklogCost = planNodeTemplate.BacklogCost + resolutionCost
                });
        }
    }

    private static void EnqueueLookupPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlog = planNodeTemplate.Backlog.Pop();

        foreach (var (schemaName, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            foreach (var lookup in compositeSchema.GetPossibleLookups(workItem.SelectionSet.Type, schemaName))
            {
                possiblePlans.Enqueue(
                    planNodeTemplate with
                    {
                        SchemaName = schemaName,
                        PathCost = planNodeTemplate.PathCost + 1,
                        BacklogCost = planNodeTemplate.BacklogCost + resolutionCost + 1,
                        Backlog = backlog.Push(workItem with { Lookup = lookup })
                    });
            }
        }
    }

    private static void EnqueueNodeLookupPlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        NodeLookupWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlog = planNodeTemplate.Backlog.Pop();
        var type = workItem.SelectionSet.Type;
        var hasEnqueuedLookup = false;

        foreach (var (schemaName, resolutionCost) in compositeSchema.GetPossibleSchemas(workItem.SelectionSet))
        {
            // If we have multiple by id lookups in a single schema,
            // we try to choose one that returns the desired type directly
            // and not an abstract type.
            var byIdLookup = compositeSchema.GetPossibleLookups(type, schemaName)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }]);

            if (byIdLookup is null)
            {
                continue;
            }

            possiblePlans.Enqueue(
                planNodeTemplate with
                {
                    SchemaName = schemaName,
                    PathCost = planNodeTemplate.PathCost + 1,
                    BacklogCost = planNodeTemplate.BacklogCost + resolutionCost + 1,
                    Backlog = backlog.Push(workItem with { Lookup = byIdLookup })
                });

            hasEnqueuedLookup = true;
        }

        // It could be that we didn't find a suitable source schema for the requested selections
        // that also has a by id resolver.
        // In this case we enqueue the best matching by id lookup of any source schema.
        if (!hasEnqueuedLookup)
        {
            var byIdLookup = compositeSchema.GetPossibleLookups(type)
                .FirstOrDefault(l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }])
                    ?? throw new InvalidOperationException(
                        $"Expected to have at least one lookup with just an 'id' argument for type '{type.Name}'.");

            possiblePlans.Enqueue(
                planNodeTemplate with
                {
                    SchemaName = byIdLookup.SchemaName,
                    PathCost = planNodeTemplate.PathCost + 1,
                    BacklogCost = planNodeTemplate.BacklogCost + 1,
                    Backlog = backlog.Push(workItem with { Lookup = byIdLookup })
                });
        }
    }

    private static void EnqueueRequirePlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        FieldRequirementWorkItem workItem,
        FusionSchemaDefinition compositeSchema)
    {
        var backlog = planNodeTemplate.Backlog.Pop();

        foreach (var schemaName in workItem.Selection.Field.Sources.Schemas)
        {
            if (schemaName == planNodeTemplate.SchemaName)
            {
                possiblePlans.Enqueue(
                    planNodeTemplate with
                    {
                        PathCost = planNodeTemplate.PathCost + 1,
                        BacklogCost = planNodeTemplate.BacklogCost,
                        Backlog = backlog.Push(workItem)
                    });

                foreach (var lookup in compositeSchema.GetPossibleLookups(workItem.Selection.Field.DeclaringType, schemaName))
                {
                    possiblePlans.Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            PathCost = planNodeTemplate.PathCost + 1,
                            BacklogCost = planNodeTemplate.BacklogCost + 1,
                            Backlog = backlog.Push(workItem with { Lookup = lookup })
                        });
                }
            }
            else
            {
                foreach (var lookup in compositeSchema.GetPossibleLookups(workItem.Selection.Field.DeclaringType, schemaName))
                {
                    possiblePlans.Enqueue(
                        planNodeTemplate with
                        {
                            SchemaName = schemaName,
                            PathCost = planNodeTemplate.PathCost + 1,
                            BacklogCost = planNodeTemplate.BacklogCost + 1,
                            Backlog = backlog.Push(workItem with { Lookup = lookup })
                        });
                }
            }
        }
    }

    public static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this FusionSchemaDefinition compositeSchema,
        SelectionSet selectionSet)
    {
        var possibleSchemas = new Dictionary<string, int>();

        CollectSchemaWeights(
            compositeSchema,
            possibleSchemas,
            selectionSet.Type,
            selectionSet.Selections);

        foreach (var (schemaName, resolvableSelections) in possibleSchemas)
        {
            // The more selections we potentially can resolve the cheaper the schema is.
            // however, if there is only a single selection left it will always get a higher
            // cost.
            yield return (schemaName, 1.0 / resolvableSelections * 2);
        }

        static void CollectSchemaWeights(
            FusionSchemaDefinition compositeSchema,
            Dictionary<string, int> possibleSchemas,
            ITypeDefinition type,
            IReadOnlyList<ISelectionNode> selections)
        {
            var complexType = type as FusionComplexTypeDefinition;

            foreach (var selection in selections)
            {
                switch (selection)
                {
                    case FieldNode fieldNode:
                        if (fieldNode.Name.Value == IntrospectionFieldNames.TypeName)
                        {
                            continue;
                        }

                        var field = complexType!.Fields[fieldNode.Name.Value];

                        if (field is { Name: "node", Type: IInterfaceTypeDefinition { Name: "Node" } })
                        {
                            continue;
                        }

                        foreach (var schemaName in field.Sources.Schemas)
                        {
                            TrackSchema(possibleSchemas, schemaName);
                        }

                        break;

                    case InlineFragmentNode inlineFragmentNode:
                        var typeCondition = type;

                        if (inlineFragmentNode.TypeCondition is not null)
                        {
                            typeCondition = compositeSchema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                        }

                        CollectSchemaWeights(
                            compositeSchema,
                            possibleSchemas,
                            typeCondition,
                            inlineFragmentNode.SelectionSet.Selections);
                        break;
                }
            }
        }

        static void TrackSchema(Dictionary<string, int> possibleSchemas, string schemaName)
        {
            if (possibleSchemas.TryGetValue(schemaName, out var count))
            {
                possibleSchemas[schemaName] = count + 1;
            }
            else
            {
                possibleSchemas[schemaName] = 1;
            }
        }
    }

    public static int NextId(this ImmutableList<PlanStep> steps)
        => steps.LastOrDefault()?.Id + 1 ?? 1;
}

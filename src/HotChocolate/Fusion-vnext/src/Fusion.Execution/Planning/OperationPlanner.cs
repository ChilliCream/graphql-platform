using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner(FusionSchemaDefinition schema)
{
    private readonly MergeSelectionSetRewriter _mergeRewriter = new(schema);
    private readonly SelectionSetPartitioner _partitioner = new(schema);

    public OperationExecutionPlan CreatePlan(OperationDefinitionNode operationDefinition)
    {
        ArgumentNullException.ThrowIfNull(operationDefinition);

        // We first need to create an index to keep track of the logical selections
        // sets before we can branch them. This allows us to inline requirements later
        // into the right place.
        var index = SelectionSetIndexer.Create(operationDefinition);
        var possiblePlans = new PriorityQueue<PlanNode, double>();

        var selectionSet = new SelectionSet(
            index.GetId(operationDefinition.SelectionSet),
            operationDefinition.SelectionSet,
            schema.GetOperationType(operationDefinition.Operation),
            SelectionPath.Root);

        var workItem = OperationWorkItem.CreateRoot(selectionSet);

        var node = new PlanNode
        {
            OperationDefinition = operationDefinition,
            SchemaName = "None",
            SelectionSetIndex = index.ToImmutable(),
            Backlog = ImmutableStack<WorkItem>.Empty.Push(workItem),
            PathCost = 1,
            BacklogCost = 1
        };

        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(selectionSet))
        {
            possiblePlans.Enqueue(
                node with
                {
                    SchemaName = schemaName,
                    BacklogCost = 1 + resolutionCost
                });
        }

        var planSteps = Plan(possiblePlans);

        if (planSteps.IsEmpty)
        {
            // if we do not get back any plan steps than there is no solution to the provided operation.
            throw new InvalidOperationException("The operation cannot be resolved.");
        }

        return BuildExecutionPlan(
            // this is not ideal and are we going to rework this once we figured out
            // introspection and defer and stream.
            planSteps.OfType<OperationPlanStep>().ToImmutableList(),
            operationDefinition);
    }

    private ImmutableList<PlanStep> Plan(PriorityQueue<PlanNode, double> possiblePlans)
    {
        while (possiblePlans.TryDequeue(out var current, out _))
        {
            var backlog = current.Backlog;

            if (backlog.IsEmpty)
            {
                // If the backlog is empty than the planning process is complete and we can return the
                // steps to build the actual execution plan.
                return current.Steps;
            }

            // The backlog represents the tasks we we have to complete to build out
            // the current possible plan. Its not guaranteed that this plan will work
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

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }
        }

        return [];
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

        // if we cannot resolve any selection with the current source schema then this path is not
        // cannot be used to resolve the data for the current operation, and we need to skip it.
        if (resolvable is null)
        {
            return;
        }

        backlog = backlog.Push(unresolvable);
        backlog = backlog.Push(fieldsWithRequirements, stepId);

        var operationBuilder =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
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

            operationBuilder.SetLookup(lookup, requirementKey);
        }

        (var definition, index) = operationBuilder.Build(index);

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Dependents = workItem.Dependents,
            Requirements = requirements
        };

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index.ToImmutable(),
            Backlog = backlog,
            Steps = current.Steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = lastRequirementId
        };

        possiblePlans.Enqueue(next, schema);
    }

    private PlanNode InlineLookupRequirements(
        SelectionSet workItemSelectionSet,
        PlanNode current,
        Lookup lookup,
        ImmutableStack<WorkItem> backlog)
    {
        var partitioner = new SelectionSetPartitioner(schema);
        var processed = new HashSet<string>();
        var lookupStepId = current.Steps.NextId();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.SelectionSet;
        index.Register(workItemSelectionSet, selectionSet);

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

            var (resolvable, unresolvable, _, _) = partitioner.Partition(input);

            if (resolvable is { Selections.Count: > 0 })
            {
                var operation =
                    InlineSelections(
                        step.Definition,
                        index,
                        step.Type,
                        index.GetId(resolvable),
                        resolvable);

                var updatedStep = step with
                {
                    Definition = operation,

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
            SelectionSetIndex = index
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
                    current,
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

        var updatedStep = currentStep with
        {
            Definition = operation,
            Requirements = requirements
        };

        steps = steps.SetItem(workItem.StepIndex, updatedStep);

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index.ToImmutable(),
            Backlog = backlog,
            Steps = steps,
            PathCost = current.PathCost,
            BacklogCost = backlog.Count()
        };

        possiblePlans.Enqueue(next, schema);
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
        var index = current.SelectionSetIndex.ToBuilder();
        var lastRequirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{lastRequirementId}";
        var requirements = ImmutableDictionary<string, OperationRequirement>.Empty;

        var leftoverRequirements =
            TryInlineFieldRequirements(
                workItem,
                current,
                currentStep,
                requirementKey,
                index,
                ref backlog,
                ref steps);

        if (leftoverRequirements is not null)
        {
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
                index,
                ref backlog);

        var selectionSetNode = new SelectionSetNode(
            [workItem.Selection.Node.WithArguments(arguments).WithSelectionSet(childSelections)]);
        index.Register(workItem.Selection.SelectionSetId, selectionSetNode);

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

        operationBuilder.SetLookup(workItem.Lookup, requirementKey);

        (var definition, _) = operationBuilder.Build(index);

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = field.DeclaringType,
            SchemaName = current.SchemaName,
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index),
            Requirements = requirements
        };

        var next = new PlanNode
        {
            Previous = current,
            OperationDefinition = current.OperationDefinition,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index.ToImmutable(),
            Backlog = backlog,
            Steps = steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count(),
            LastRequirementId = lastRequirementId
        };

        possiblePlans.Enqueue(next, schema);
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
        PlanNode current,
        OperationPlanStep currentStep,
        string requirementKey,
        SelectionSetIndexBuilder index,
        ref ImmutableStack<WorkItem> backlog,
        ref ImmutableList<PlanStep> steps)
    {
        var field = workItem.Selection.Field;
        var fieldSource = field.Sources[current.SchemaName];
        // todo: we need a deep copy of this selection set or there might be problems if a requirement
        // is used on different parts of the operation.
        var requirements = fieldSource.Requirements!.SelectionSet;

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
        SelectionSetNode selectionsToInline)
    {
        var rewriter = SyntaxRewriter.Create<Stack<ISyntaxNode>>(
            (node, path) =>
            {
                if (node is SelectionSetNode selectionSet)
                {
                    var originalSelectionSet = (SelectionSetNode)path.Peek();
                    var id = index.GetId(originalSelectionSet);

                    if (!ReferenceEquals(originalSelectionSet, selectionSet))
                    {
                        index.Register(originalSelectionSet, selectionSet);
                    }

                    if (targetSelectionSetId == id)
                    {
                        var newSelectionSet = _mergeRewriter.Merge(selectionSet, selectionsToInline, selectionSetType);
                        index.Register(originalSelectionSet, newSelectionSet);
                        return newSelectionSet;
                    }
                }

                return node;
            },
            (node, path) =>
            {
                path.Push(node);
                return path;
            },
            (_, path) => path.Pop());

        return (OperationDefinitionNode)rewriter.Rewrite(operation, new Stack<ISyntaxNode>())!;
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
                && step.SelectionSets.Contains(selectionSetId))
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

    public static ISelectionSetIndex ToImmutable(this ISelectionSetIndex index)
    {
        if (index is SelectionSetIndexBuilder builder)
        {
            return builder.Build();
        }

        return index;
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
                possiblePlans.Enqueue(planNodeTemplate);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                possiblePlans.EnqueueRootPlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            case OperationWorkItem { Kind: OperationWorkItemKind.Lookup } wi:
                possiblePlans.EnqueueLookupPlanNodes(planNodeTemplate, wi, compositeSchema);
                break;

            case FieldRequirementWorkItem wi:
                possiblePlans.EnqueueRequirePlanNodes(planNodeTemplate, wi);
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
            foreach (var lookup in workItem.SelectionSet.Type.GetPossibleLookups(schemaName))
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

    private static void EnqueueRequirePlanNodes(
        this PriorityQueue<PlanNode, double> possiblePlans,
        PlanNode planNodeTemplate,
        FieldRequirementWorkItem workItem)
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

                foreach (var lookup in workItem.Selection.Field.DeclaringType.GetPossibleLookups(schemaName))
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
                foreach (var lookup in workItem.Selection.Field.DeclaringType.GetPossibleLookups(schemaName))
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
                        foreach (var schemaName in complexType!.Fields[fieldNode.Name.Value].Sources.Schemas)
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

    private static IEnumerable<Lookup> GetPossibleLookups(this ITypeDefinition type, string schemaName)
    {
        if (type is FusionComplexTypeDefinition complexType
            && complexType.Sources.TryGetType(schemaName, out var source))
        {
            return source.Lookups;
        }

        return [];
    }

    public static int NextId(this ImmutableList<PlanStep> steps)
        => steps.LastOrDefault()?.Id + 1 ?? 1;
}

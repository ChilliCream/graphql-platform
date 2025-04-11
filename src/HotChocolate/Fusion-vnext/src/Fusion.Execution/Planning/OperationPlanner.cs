using System.Collections.Immutable;
using System.Globalization;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed class OperationPlanner(FusionSchemaDefinition schema)
{
    private readonly MergeSelectionSetRewriter _mergeRewriter = new(schema);
    private readonly SelectionSetPartitioner _partitioner = new(schema);

    public ImmutableList<PlanStep> CreatePlan(OperationDefinitionNode operation)
    {
        var index = SelectionSetIndexer.Create(operation);
        var openSet = new SortedSet<PlanNode>(Comparer<PlanNode>.Create(
            (a, b) => a.TotalCost.CompareTo(b.TotalCost)));

        var selectionSet = new SelectionSet(
            index.GetId(operation.SelectionSet),
            operation.SelectionSet,
            schema.GetOperationType(operation.Operation),
            SelectionPath.Root);

        var workItem = OperationWorkItem.CreateRoot(selectionSet);

        var node = new PlanNode
        {
            SchemaName = "None",
            SelectionSetIndex = index.ToImmutable(),
            Backlog = ImmutableStack<WorkItem>.Empty.Push(workItem),
            PathCost = 1,
            BacklogCost = 1,
        };

        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(selectionSet))
        {
            openSet.Add(node with { SchemaName = schemaName, BacklogCost = 1 + resolutionCost });
        }

        return Plan(openSet);
    }

    private ImmutableList<PlanStep> Plan(SortedSet<PlanNode> openSet)
    {
        while (openSet.Count != 0)
        {
            var current = openSet.First();
            openSet.Remove(current);

            var backlog = current.Backlog;
            if (backlog.IsEmpty)
            {
                return current.Steps;
            }

            backlog = current.Backlog.Pop(out var workItem);

            switch (workItem)
            {
                case OperationWorkItem { Kind: OperationWorkItemKind.Root } wi:
                    PlanSelections(openSet, backlog, current, wi, null);
                    break;

                case OperationWorkItem { Kind: OperationWorkItemKind.Lookup, Lookup: { } lookup } wi:
                    current = InlineLookupRequirements(backlog, current, wi, lookup);
                    PlanSelections(openSet, backlog, current, wi, lookup);
                    break;

                case FieldWithRequirementWorkItem { Lookup: null } wi:
                    TryInlineFieldWithRequirements(openSet, backlog, current, wi);
                    break;

                case FieldWithRequirementWorkItem wi:
                    break;

                case FieldRequirementsWorkItem wi:
                    break;

                default:
                    throw new NotSupportedException(
                        "The work item type is not supported.");
            }
        }

        return ImmutableList<PlanStep>.Empty;
    }

    private void PlanSelections(
        SortedSet<PlanNode> openSet,
        ImmutableStack<WorkItem> backlog,
        PlanNode current,
        OperationWorkItem workItem,
        Lookup? lookup)
    {
        var stepId = current.Steps.LastOrDefault()?.Id + 1 ?? 1;
        var index = current.SelectionSetIndex;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName, SelectionSet = workItem.SelectionSet, SelectionSetIndex = index,
        };

        (var resolvable, var unresolvable, var fieldsWithRequirements, index) = _partitioner.Partition(input);

        // if we cannot resolve any selection with the current source schema then this path is not
        // cannot be used to resolve the data for the current operation and we need to skip it.
        if (resolvable is null)
        {
            return;
        }

        backlog = backlog.Push(unresolvable);
        backlog = backlog.Push(fieldsWithRequirements, stepId);

        (var definition, index) =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetSelectionSet(resolvable)
                .SetLookup(lookup)
                .Build(index);

        var step = new OperationPlanStep
        {
            Id = stepId,
            Definition = definition,
            Type = workItem.SelectionSet.Type,
            SchemaName = current.SchemaName,
            SelectionSets = SelectionSetIndexer.CreateIdSet(definition.SelectionSet, index)
        };

        var next = new PlanNode
        {
            Previous = current,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index.ToImmutable(),
            Backlog = backlog,
            Steps = current.Steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count()
        };

        openSet.AddPlanNodes(next, schema);
    }

    private PlanNode InlineLookupRequirements(
        ImmutableStack<WorkItem> backlog,
        PlanNode current,
        OperationWorkItem workItem,
        Lookup lookup)
    {
        var partitioner = new SelectionSetPartitioner(schema);
        var processed = new HashSet<string>();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.SelectionSet;
        index.Register(workItem.SelectionSet, selectionSet);

        foreach (var (step, stepIndex, schemaName) in current.GetCandidateSteps(workItem.SelectionSet.Id))
        {
            if (!processed.Add(schemaName))
            {
                continue;
            }

            var input = new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                SelectionSet = workItem.SelectionSet with { Node = selectionSet },
                SelectionSetIndex = index
            };

            var (resolvable, unresolvable, _, _) = partitioner.Partition(input);

            if (resolvable is not null)
            {
                var operation =
                    OperationRewriterUtils.Inline(
                        _mergeRewriter,
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
                    Dependents = step.Dependents.Add(current.Steps.Count)
                };

                steps = steps.SetItem(stepIndex, updatedStep);
            }

            selectionSet = null;

            if (!unresolvable.IsEmpty)
            {
                var top = unresolvable.Peek();
                if (top.Id == workItem.SelectionSet.Id)
                {
                    unresolvable = unresolvable.Pop(out top);
                    selectionSet = top.Node;
                }

                backlog = backlog.Push(unresolvable);
            }

            if (selectionSet is null)
            {
                break;
            }
        }

        // if we have still selections left we need to add them to the backlog.
        if (selectionSet is not null)
        {
            var requirements = workItem with
            {
                Kind = OperationWorkItemKind.Lookup,
                Lookup = null,
                SelectionSet = workItem.SelectionSet with { Node = selectionSet },
                Dependents = workItem.Dependents.Add(current.Steps.Count)
            };

            backlog = backlog.Push(requirements);
        }

        return current with { Steps = steps, Backlog = backlog, SelectionSetIndex = index };
    }

    private void TryInlineFieldWithRequirements(
        SortedSet<PlanNode> openSet,
        ImmutableStack<WorkItem> backlog,
        PlanNode current,
        FieldWithRequirementWorkItem workItem)
    {
        if (current.Steps.ById(workItem.StepId) is not OperationPlanStep currentStep)
        {
            return;
        }

        var partitioner = new SelectionSetPartitioner(schema);
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var requirementId = current.LastRequirementId + 1;
        var requirementKey = $"__fusion_{requirementId}";
        var field = workItem.Selection.Field;
        var fieldSource = field.Sources[current.SchemaName];
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

            var (resolvable, unresolvable, _, _) = partitioner.Partition(input);

            if (resolvable is not null)
            {
                var operation =
                    OperationRewriterUtils.Inline(
                        _mergeRewriter,
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
            }

            requirements = null;

            if (!unresolvable.IsEmpty)
            {
                var top = unresolvable.Peek();
                if (top.Id == workItem.Selection.SelectionSetId)
                {
                    unresolvable = unresolvable.Pop(out top);
                    requirements = top.Node;
                }

                foreach (var selectionSet in unresolvable.Reverse())
                {
                    backlog = backlog.Push(
                        new FieldRequirementsWorkItem(
                            requirementKey,
                            workItem.StepId,
                            selectionSet));
                }
            }

            if (requirements is null)
            {
                break;
            }
        }

        if (requirements is null)
        {
            var arguments = new List<ArgumentNode>(workItem.Selection.Node.Arguments);

            foreach (var argument in fieldSource.Requirements.Arguments)
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
                OperationRewriterUtils.Inline(
                    _mergeRewriter,
                    currentStep.Definition,
                    index,
                    currentStep.Type,
                    workItem.Selection.SelectionSetId,
                    new SelectionSetNode([workItem.Selection.Node.WithArguments(arguments)]));

            var updatedStep = currentStep with
            {
                Definition = operation
            };

            steps = steps.SetItem(workItem.StepIndex, updatedStep);

            var next = new PlanNode
            {
                Previous = current,
                SchemaName = current.SchemaName,
                SelectionSetIndex = index.ToImmutable(),
                Backlog = backlog,
                Steps = steps,
                PathCost = current.PathCost,
                BacklogCost = backlog.Count()
            };

            openSet.AddPlanNodes(next, schema);
        }
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
            var workItem = new FieldWithRequirementWorkItem(selection, stepId);
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

    public static void AddPlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        FusionSchemaDefinition schema)
    {
        var nextWorkItem = planNodeTemplate.Backlog.IsEmpty
            ? null
            : planNodeTemplate.Backlog.Peek();

        if (nextWorkItem is null)
        {
            openSet.Add(planNodeTemplate);
        }
        else if (nextWorkItem is OperationWorkItem { Kind: OperationWorkItemKind.Root } rwi)
        {
            openSet.AddRootPlanNodes(planNodeTemplate, rwi, schema);
        }
        else if (nextWorkItem is OperationWorkItem { Kind: OperationWorkItemKind.Lookup } lwi)
        {
            openSet.AddLookupPlanNodes(planNodeTemplate, lwi, schema);
        }
        else if (nextWorkItem is FieldWithRequirementWorkItem frwi)
        {
            openSet.AddRequirePlanNodes(planNodeTemplate, frwi);
        }
        else
        {
            throw new NotSupportedException(
                "The work item type is not supported.");
        }
    }

    private static void AddRootPlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition schema)
    {
        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem))
        {
            openSet.Add(planNodeTemplate with
            {
                SchemaName = schemaName,
                PathCost = planNodeTemplate.PathCost + 1,
                BacklogCost = planNodeTemplate.BacklogCost + resolutionCost
            });
        }
    }

    private static void AddLookupPlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        OperationWorkItem workItem,
        FusionSchemaDefinition schema)
    {
        var backlog = planNodeTemplate.Backlog.Pop();

        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem))
        {
            foreach (var lookup in workItem.SelectionSet.Type.GetPossibleLookups(schemaName))
            {
                openSet.Add(planNodeTemplate with
                {
                    SchemaName = schemaName,
                    PathCost = planNodeTemplate.PathCost + 1,
                    BacklogCost = planNodeTemplate.BacklogCost + resolutionCost + 1,
                    Backlog = backlog.Push(workItem with { Lookup = lookup })
                });
            }
        }
    }

    private static void AddRequirePlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        FieldWithRequirementWorkItem workItem)
    {
        var backlog = planNodeTemplate.Backlog.Pop();

        foreach (var schemaName in workItem.Selection.Field.Sources.Schemas)
        {
            if (schemaName == planNodeTemplate.SchemaName)
            {
                openSet.Add(planNodeTemplate with
                {
                    PathCost = planNodeTemplate.PathCost + 1,
                    BacklogCost = planNodeTemplate.BacklogCost,
                    Backlog = backlog.Push(workItem)
                });
            }
            else
            {
                foreach (var lookup in workItem.Selection.Field.DeclaringType.GetPossibleLookups(schemaName))
                {
                    openSet.Add(planNodeTemplate with
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


    private static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this FusionSchemaDefinition schema,
        OperationWorkItem workItem)
        => schema.GetPossibleSchemas(workItem.SelectionSet);

    public static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this FusionSchemaDefinition schema,
        SelectionSet selectionSet)
    {
        var possibleSchemas = new Dictionary<string, int>();
        CollectSchemaWeights(
            schema,
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
            FusionSchemaDefinition schema,
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
                            typeCondition = schema.Types[inlineFragmentNode.TypeCondition.Name.Value];
                        }

                        CollectSchemaWeights(
                            schema,
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
}

file class OperationDefinitionBuilder
{
    private OperationType _type = OperationType.Query;
    private Lookup? _lookup;
    private SelectionSetNode? _selectionSet;

    private OperationDefinitionBuilder()
    {
    }

    public static OperationDefinitionBuilder New()
        => new();

    public OperationDefinitionBuilder SetType(OperationType type)
    {
        _type = type;
        return this;
    }

    public OperationDefinitionBuilder SetLookup(Lookup? lookup)
    {
        _lookup = lookup;
        return this;
    }

    public OperationDefinitionBuilder SetSelectionSet(SelectionSetNode selectionSet)
    {
        _selectionSet = selectionSet;
        return this;
    }

    public (OperationDefinitionNode, ISelectionSetIndex) Build(ISelectionSetIndex index)
    {
        if (_selectionSet is null)
        {
            throw new InvalidOperationException("The operation selection set must be specified.");
        }

        var selectionSet = _selectionSet;

        if (_lookup is not null)
        {
            var lookupField = new FieldNode(
                new NameNode(_lookup.Name),
                null,
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                selectionSet);

            selectionSet = new SelectionSetNode(null, [lookupField]);

            var indexBuilder = index.ToBuilder();
            indexBuilder.Register(selectionSet);
            index = indexBuilder;
        }

        var definition = new OperationDefinitionNode(
            null,
            null,
            _type,
            Array.Empty<VariableDefinitionNode>(),
            Array.Empty<DirectiveNode>(),
            selectionSet);

        return (definition, index);
    }
}

file static class OperationRewriterUtils
{
    public static OperationDefinitionNode Inline(
        MergeSelectionSetRewriter mergeRewriter,
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
                        var newSelectionSet = mergeRewriter.Merge(selectionSet, selectionsToInline, selectionSetType);
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

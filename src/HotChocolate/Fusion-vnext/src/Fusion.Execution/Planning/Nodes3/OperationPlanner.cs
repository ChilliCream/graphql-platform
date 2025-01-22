using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Planning.Nodes3;

public class OperationPlanner(CompositeSchema schema)
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

        var workItem = new WorkItem(
            WorkItemKind.Root,
            selectionSet);

        var node = new PlanNode
        {
            SchemaName = "None",
            SelectionSetIndex = index.ToImmutable(),
            Backlog = ImmutableStack<WorkItem>.Empty.Push(workItem),
            PathCost = 1,
            BacklogCost = 1,
        };

        foreach (var (schemaName, resolutionCost) in  schema.GetPossibleSchemas(selectionSet))
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

            switch (workItem.Kind)
            {
                case WorkItemKind.Root:
                    PlanSelections(openSet, backlog, current, workItem);
                    break;

                case WorkItemKind.Lookup:
                    current = InlineLookupRequirements(backlog, current, workItem, workItem.Lookup!);
                    PlanSelections(openSet, backlog, current, workItem);
                    break;

                case WorkItemKind.Requirement:
                    InlineFieldRequirement(backlog, current, workItem);
                    break;
            }
        }

        return ImmutableList<PlanStep>.Empty;
    }

    private void PlanSelections(
        SortedSet<PlanNode> openSet,
        ImmutableStack<WorkItem> backlog,
        PlanNode current,
        WorkItem workItem)
    {
        var index = current.SelectionSetIndex;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            SelectionSet = workItem.SelectionSet,
            SelectionSetIndex = index,
        };

        (var resolvable, var unresolvable, var fields, index) = _partitioner.Partition(input);

        if (resolvable is null)
        {
            return;
        }

        backlog = backlog.Push(unresolvable);

        (var definition, index) =
            OperationDefinitionBuilder
                .New()
                .SetType(OperationType.Query)
                .SetSelectionSet(resolvable)
                .SetLookup(workItem.Lookup)
                .Build(index);

        var step = new OperationPlanStep
        {
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
        WorkItem workItem,
        Lookup lookup)
    {
        var partitioner = new SelectionSetPartitioner(schema);
        var processed = new HashSet<string>();
        var steps = current.Steps;
        var index = current.SelectionSetIndex.ToBuilder();
        var selectionSet = lookup.SelectionSet;
        index.Register(workItem.SelectionSet, selectionSet);

        foreach (var (step, stepIndex, schemaName) in GetPossibleSteps(current, workItem.SelectionSet.Id))
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

            var (resolvable, unresolvable, fields, _) = partitioner.Partition(input);

            if (resolvable is not null)
            {
                var operation =
                    Inline(
                        step.Type,
                        step.Definition,
                        resolvable,
                        _mergeRewriter,
                        index,
                        index.GetId(resolvable));

                var updatedStep = step with
                {
                    Definition = operation,

                    // we add the new lookup node to the dependants of the current step.
                    // the new lookup node will be the next index added which is the last index aka Count.
                    Dependants = step.Dependants.Add(current.Steps.Count)
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
                Kind = WorkItemKind.Lookup,
                Lookup = null,
                SelectionSet = workItem.SelectionSet with { Node = selectionSet },
                Dependants = workItem.Dependants.Add(current.Steps.Count)
            };

            backlog = backlog.Push(requirements);
        }

        return current with { Steps = steps, Backlog = backlog, SelectionSetIndex = index };

        static IEnumerable<(OperationPlanStep, int, string)> GetPossibleSteps(PlanNode current, uint selectionSetId)
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

        static OperationDefinitionNode Inline(
            ICompositeNamedType type,
            OperationDefinitionNode operation,
            SelectionSetNode requirements,
            MergeSelectionSetRewriter mergeRewriter,
            SelectionSetIndexBuilder index,
            uint targetSelectionSetId)
        {
            var rewriter = SyntaxRewriter.Create<Stack<ISyntaxNode>>(
                (node, path) =>
                {
                    if (node is SelectionSetNode selectionSet)
                    {
                        var originalSelectionSet = (SelectionSetNode)path.Peek();
                        var id = index.GetId(originalSelectionSet);

                        if(!ReferenceEquals(originalSelectionSet, selectionSet))
                        {
                            index.Register(originalSelectionSet, selectionSet);
                        }

                        if (targetSelectionSetId == id)
                        {
                            return mergeRewriter.Merge(selectionSet, requirements, type);
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

    private void InlineFieldRequirement(
        ImmutableStack<WorkItem> backlog,
        PlanNode current,
        WorkItem workItem)
    {

    }
}

file static class Extensions
{
    public static ImmutableStack<WorkItem> Push(
        this ImmutableStack<WorkItem> backlog,
        ImmutableStack<SelectionSet> unresolvable)
    {
        //string schemaName = null!;
        // CompositeSchema schema = null!;

        if(unresolvable.IsEmpty)
        {
            return backlog;
        }

        foreach (var selectionSet in unresolvable.Reverse())
        {
            if (selectionSet.Selections is [FieldNode fieldNode])
            {

            }


            var workItem = new WorkItem(
                selectionSet.Path.IsRoot
                    ? WorkItemKind.Root
                    : WorkItemKind.Lookup,
                selectionSet);
            backlog = backlog.Push(workItem);
        }

        return backlog;
    }

    public static ISelectionSetIndex ToImmutable(this ISelectionSetIndex index)
    {
        if(index is SelectionSetIndexBuilder builder)
        {
            return builder.Build();
        }

        return index;
    }

    public static void AddPlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        CompositeSchema schema)
    {
        var nextWorkItem = planNodeTemplate.Backlog.IsEmpty
            ? null
            : planNodeTemplate.Backlog.Peek();

        if (nextWorkItem is null)
        {
            openSet.Add(planNodeTemplate);
        }
        else if (nextWorkItem.Kind == WorkItemKind.Root)
        {
            openSet.AddRootPlanNodes(planNodeTemplate, nextWorkItem, schema);
        }
        else
        {
            openSet.AddLookupPlanNodes(planNodeTemplate, nextWorkItem, schema);
        }
    }

    private static void AddRootPlanNodes(
        this SortedSet<PlanNode> openSet,
        PlanNode planNodeTemplate,
        WorkItem workItem,
        CompositeSchema schema)
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
        WorkItem workItem,
        CompositeSchema schema)
    {
        var backlog = planNodeTemplate.Backlog.Pop();

        foreach (var (schemaName, resolutionCost) in schema.GetPossibleSchemas(workItem))
        {
            foreach (var lookup in  workItem.SelectionSet.Type.GetPossibleLookups(schemaName))
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

    private static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this CompositeSchema schema,
        WorkItem workItem)
        => GetPossibleSchemas(schema, workItem.SelectionSet);

    public static IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        this CompositeSchema schema,
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
            yield return (schemaName, (1.0 / resolvableSelections) * 2);
        }

        static void CollectSchemaWeights(
            CompositeSchema schema,
            Dictionary<string, int> possibleSchemas,
            ICompositeNamedType type,
            IReadOnlyList<ISelectionNode> selections)
        {
            var complexType = type as CompositeComplexType;

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
                            typeCondition = schema.GetType(inlineFragmentNode.TypeCondition.Name.Value);
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

    private static IEnumerable<Lookup> GetPossibleLookups(this ICompositeNamedType type, string schemaName)
    {
        if (type is CompositeComplexType complexType
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

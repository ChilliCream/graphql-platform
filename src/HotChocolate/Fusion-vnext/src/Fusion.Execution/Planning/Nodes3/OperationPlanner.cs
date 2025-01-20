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
        var rootType = schema.GetOperationType(operation.Operation);
        var selections = operation.SelectionSet.Selections;
        var selectionSetIndex = SelectionSetIndexer.Create(operation);
        var openSet = new SortedSet<PlanNode>(Comparer<PlanNode>.Create(
            (a, b) => a.TotalCost.CompareTo(b.TotalCost)));

        var workItem = new BacklogItem(
            PlanNodeKind.Root,
            SelectionPath.Root,
            operation,
            operation.SelectionSet,
            selectionSetIndex.GetSelectionSetId(operation.SelectionSet),
            rootType);

        var node = new PlanNode
        {
            Kind = PlanNodeKind.Root,
            Path = SelectionPath.Root,
            SchemaName = "None",
            SelectionSetIndex = selectionSetIndex,
            Backlog = ImmutableStack<BacklogItem>.Empty.Push(workItem),
            PathCost = 1,
            BacklogCost = 1,
        };

        foreach (var (schemaName, resolutionCost) in GetPossibleSchemas(rootType, selections))
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

            switch (current.Kind)
            {
                case PlanNodeKind.Root:
                    PlanRootOperation(openSet, backlog, current, workItem);
                    break;

                case PlanNodeKind.InlineLookupRequirements:
                    InlineLookupRequirements(openSet, backlog, current, workItem, current.Lookup!);
                    break;
            }
        }

        return ImmutableList<PlanStep>.Empty;
    }

    private void PlanRootOperation(
        SortedSet<PlanNode> openSet,
        ImmutableStack<BacklogItem> backlog,
        PlanNode current,
        BacklogItem workItem)
    {
        var index = current.SelectionSetIndex;
        var rootType = (CompositeObjectType)workItem.Type;

        var input = new SelectionSetPartitionerInput
        {
            SchemaName = current.SchemaName,
            Type = rootType,
            SelectionSetNode = workItem.SelectionSet,
            SelectionPath = SelectionPath.Root,
        };

        var (resolvable, _) = _partitioner.Partition(input, ref index, ref backlog);

        if (resolvable is null)
        {
            return;
        }

        var step = new OperationPlanStep
        {
            Definition = new OperationDefinitionNode(
                null,
                null,
                OperationType.Query,
                Array.Empty<VariableDefinitionNode>(),
                Array.Empty<DirectiveNode>(),
                resolvable),
            Type = rootType,
            SchemaName = current.SchemaName,
            SelectionSets = SelectionSetIndexer.CreateIdSet(resolvable, index)
        };

        var nextWorkItem = backlog.IsEmpty ? null : backlog.Peek();
        var kind = nextWorkItem?.Kind ?? PlanNodeKind.Root;
        var path = nextWorkItem?.Path ?? SelectionPath.Root;

        var next = new PlanNode
        {
            Previous = current,
            Kind = kind,
            Path = path,
            SchemaName = current.SchemaName,
            SelectionSetIndex = index,
            Backlog = backlog,
            Steps = current.Steps.Add(step),
            PathCost = current.PathCost,
            BacklogCost = backlog.Count()
        };

        if (nextWorkItem is null)
        {
            openSet.Add(next);
        }
        else if (nextWorkItem.Kind == PlanNodeKind.Root)
        {
            foreach (var (schemaName, resolutionCost) in GetPossibleSchemas(nextWorkItem))
            {
                openSet.Add(next with
                {
                    SchemaName = schemaName,
                    PathCost = next.PathCost + 1,
                    BacklogCost = next.BacklogCost + resolutionCost
                });
            }
        }
        else
        {
            foreach (var (schemaName, resolutionCost) in GetPossibleSchemas(nextWorkItem))
            {
                foreach (var lookup in GetPossibleLookups(nextWorkItem.Type, schemaName))
                {
                    openSet.Add(next with
                    {
                        SchemaName = schemaName,
                        PathCost = next.PathCost + 1,
                        Lookup = lookup,
                        BacklogCost = next.BacklogCost + resolutionCost + 1,
                    });
                }
            }
        }
    }

    private void InlineLookupRequirements(
        SortedSet<PlanNode> openSet,
        ImmutableStack<BacklogItem> backlog,
        PlanNode current,
        BacklogItem workItem,
        Lookup lookup)
    {
        var partitioner = new SelectionSetPartitioner(schema);
        var processed = new HashSet<string>();
        var next = current;
        var steps = current.Steps;
        var index = current.SelectionSetIndex.Branch();
        var selectionSet = lookup.SelectionSet;
        index.RegisterSelectionSet(workItem.SelectionSet, selectionSet);
        backlog = backlog.Push(workItem with { Kind = PlanNodeKind.ResolveLookupSelections });

        foreach (var (step, stepIndex, schemaName) in GetPossibleSteps(current, workItem.SelectionSetId))
        {
            if (!processed.Add(schemaName))
            {
                continue;
            }

            var input = new SelectionSetPartitionerInput
            {
                SchemaName = schemaName,
                Type = workItem.Type,
                SelectionSetNode = selectionSet,
                SelectionPath = workItem.Path,
                AllowRequirements = false
            };

            var (resolvable, unresolvable) = partitioner.Partition(input, ref index, ref backlog);

            if (resolvable is not null)
            {
                var operation =
                    InlineRequirements(
                        step.Type,
                        step.Definition,
                        resolvable,
                        _mergeRewriter,
                        index,
                        index.GetSelectionSetId(resolvable));

                steps = steps.SetItem(stepIndex, step with { Definition = operation });
            }

            selectionSet = unresolvable;

            if (selectionSet is null)
            {
                break;
            }
        }

        openSet.Add(
            next with
            {
                Kind = backlog.Peek().Kind,
                Steps = steps,
                Backlog = backlog
            });

        static IEnumerable<(OperationPlanStep, int, string)> GetPossibleSteps(PlanNode current, int selectionSetId)
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

        static OperationDefinitionNode InlineRequirements(
            ICompositeNamedType type,
            OperationDefinitionNode operation,
            SelectionSetNode requirements,
            MergeSelectionSetRewriter mergeRewriter,
            SelectionSetIndex index,
            int targetSelectionSetId)
        {
            var rewriter = SyntaxRewriter.Create<Stack<ISyntaxNode>>(
                (node, path) =>
                {
                    if (node is SelectionSetNode selectionSet)
                    {
                        var originalSelectionSet = (SelectionSetNode)path.Peek();
                        var id = index.GetSelectionSetId(originalSelectionSet);

                        if(!ReferenceEquals(originalSelectionSet, selectionSet))
                        {
                            index.RegisterSelectionSet(originalSelectionSet, selectionSet);
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

    private IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        BacklogItem workItem)
        => GetPossibleSchemas(workItem.Type, workItem.SelectionSet.Selections);

    private IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        ICompositeNamedType type,
        IReadOnlyList<ISelectionNode> selections)
    {
        var possibleSchemas = new Dictionary<string, int>();
        CollectSchemaWeights(
            schema,
            possibleSchemas,
            type,
            selections);

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

    private IEnumerable<Lookup> GetPossibleLookups(ICompositeNamedType type, string schemaName)
    {
        if (type is CompositeComplexType complexType
            && complexType.Sources.TryGetType(schemaName, out var source))
        {
            return source.Lookups;
        }

        return [];
    }
}

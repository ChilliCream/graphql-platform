using System.Collections.Immutable;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public class OperationPlanner(CompositeSchema schema)
{
    private PlanNode? PlanSelectionSet(SortedSet<PlanNode> openSet, SelectionSetIndex selectionSetIndex)
    {
        var nextId = 1;

        while (openSet.Count != 0)
        {
            var current = openSet.First();
            openSet.Remove(current);

            if (current.Backlog.IsEmpty)
            {
                return current;
            }

            var backlog = current.Backlog.Dequeue(out var workItem);
            var type = workItem.Type;

            switch (workItem.Node.Kind)
            {
                case SyntaxKind.OperationDefinition:
                {
                    var rootType = (CompositeObjectType)type;

                    var selectionSetNode = PlanRootSelections(
                        rootType,
                        workItem.SelectionSet,
                        current.NextSchemaName,
                        selectionSetIndex,
                        ref backlog);

                    if (selectionSetNode is null)
                    {
                        // this path is not possible
                        continue;
                    }

                    var operation = new OperationPlanNode
                    {
                        Id = nextId++,
                        Previous = current,
                        Path = current.Path,
                        SchemaName = current.SchemaName,
                        NextSchemaName = current.NextSchemaName,
                        PathCost = current.PathCost + 1,
                        BacklogCost = backlog.Count(),
                        Backlog = backlog,
                        SelectionSet = selectionSetNode,
                        Type = rootType
                    };

                    foreach (var (schemaName, resolutionCost) in GetPossibleSchemas(backlog))
                    {
                        openSet.Add(operation with
                        {
                            NextSchemaName = schemaName,
                            BacklogCost = operation.BacklogCost + resolutionCost
                        });
                    }

                    break;
                }
                case SyntaxKind.Field:
                {
                    break;
                }
                case SyntaxKind.InlineFragment:
                {
                    break;
                }
                default:
                    throw new InvalidOperationException();
            }
        }

        return null;
    }

    private SelectionSetNode? PlanRootSelections(
        CompositeObjectType rootType,
        SelectionSetNode selectionSetNode,
        string schemaName,
        SelectionSetIndex selectionSetIndex,
        ref ImmutableQueue<BacklogItem> backlog)
    {
        var input = new SelectionSetPartitionerInput
        {
            SchemaName = schemaName,
            Type = rootType,
            SelectionSetNode = selectionSetNode,
            SelectionSetIndex = selectionSetIndex,
            SelectionPath = SelectionPath.Root,
            AllowRequirements = false
        };

        var partitioner = new SelectionSetPartitioner(schema);
        return partitioner.Partition(input, ref backlog);
    }

    private SelectionSetNode? PlanLookupSelections(
        ICompositeNamedType type,
        IReadOnlyList<ISelectionNode> selections,
        string schemaName,
        SelectionSetIndex selectionSetIndex,
        ref ImmutableQueue<BacklogItem> backlog)
    {
        var input = new SelectionSetPartitionerInput
        {
            SchemaName = schemaName,
            Type = type,
            SelectionSetNode = new SelectionSetNode(selections),
            SelectionSetIndex = selectionSetIndex,
            SelectionPath = SelectionPath.Root,
            AllowRequirements = false
        };

        var partitioner = new SelectionSetPartitioner(schema);
        return partitioner.Partition(input, ref backlog);
    }

    private IEnumerable<(string SchemaName, double Cost)> GetPossibleSchemas(
        ImmutableQueue<BacklogItem> backlog)
    {
        if (backlog.IsEmpty)
        {
            yield break;
        }

        var possibleSchemas = new Dictionary<string, int>();
        var workItem = backlog.Peek();
        CollectSchemaWeights(
            schema,
            possibleSchemas,
            workItem.Type,
            workItem.SelectionSet.Selections);

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
            if(possibleSchemas.TryGetValue(schemaName, out var count))
            {
                possibleSchemas[schemaName] = count + 1;
            }
            else
            {
                possibleSchemas[schemaName] = 1;
            }
        }
    }
}

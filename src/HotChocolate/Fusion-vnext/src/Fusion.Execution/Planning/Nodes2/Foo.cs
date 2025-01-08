using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes2;

public abstract class PlanNode
{
    public PlanNode? Previous { get; init; }

    public abstract ISyntaxNode SyntaxNode { get; }

    public required SelectionPath Path { get; init; }

    public required string SchemaName { get; init; }

    public int PathCost { get; init; }

    public required ImmutableSortedSet<BacklogItem> Backlog { get; init; }

    public int TotalCost => PathCost + Backlog.Count;
}

public class FieldPlanNode : PlanNode
{
    public required FieldNode FieldNode { get; init; }

    public override ISyntaxNode SyntaxNode => FieldNode;

    public required CompositeOutputField Field { get; init; }
}

public class LookupPlanNode : PlanNode
{
    public required SelectionSetNode SelectionSetNode { get; init; }

    public override ISyntaxNode SyntaxNode => SelectionSetNode;

    public required Lookup Lookup { get; init; }
}


public sealed record BacklogItem(int Priority, ISyntaxNode Parent, ISyntaxNode Node, string? IgnoreSchemaName = null);



public class Planner(CompositeSchema schema)
{
    private PlanNode PlanSelectionSet(SortedSet<PlanNode> openSet)
    {
        while (openSet.Any())
        {
            var current = openSet.First();
            openSet.Remove(current);

            if (current.Backlog.IsEmpty)
            {
                return current;
            }

            var next = current.Backlog.First();
            var type = GetCurrentTypeContext(current, next);

            switch (next.Node)
            {
                case FieldNode fieldNode:
                    var complexType = (CompositeComplexType)type;
                    var field = complexType.Fields[fieldNode.Name.Value];
                    var backlogBase = current.Backlog.Remove(next);

                    foreach (var source in field.Sources)
                    {
                        if(source.SchemaName.Equals(next.IgnoreSchemaName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var cost = current.PathCost + 1;
                        var backlog = backlogBase;

                        if (source.SchemaName == current.SchemaName)
                        {
                            if (source.Requirements is not null)
                            {
                                foreach (var requirement in source.Requirements.SelectionSet.Selections)
                                {
                                    backlog = backlog.Add(new BacklogItem(-1, current.SyntaxNode, requirement, source.SchemaName));
                                }
                            }

                            var fieldPlanNode = new FieldPlanNode
                            {
                                Previous = current,
                                Path = current.Path.AppendField(fieldNode.Name.Value),
                                SchemaName = current.SchemaName,
                                PathCost = cost,
                                Backlog = backlog,
                                FieldNode = fieldNode,
                                Field = field,
                            };

                            openSet.Add(fieldPlanNode);
                        }
                        else
                        {
                            cost += 10;

                            if (!complexType.Sources.TryGetType(source.SchemaName, out var sourceType)
                                || sourceType.Lookups.Length == 0)
                            {
                                continue;
                            }

                            foreach (var VARIABLE in complexType.Sources[])
                            {
                            }
                        }
                    }
                    break;
            }




        }

    }

    private ICompositeNamedType GetCurrentTypeContext(PlanNode node, BacklogItem backlogItem)
    {
        throw new InvalidOperationException();
    }
}

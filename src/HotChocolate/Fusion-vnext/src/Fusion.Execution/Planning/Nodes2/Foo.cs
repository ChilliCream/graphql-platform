using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes2;

public abstract class PlanNode
{
    public PlanNode? Previous { get; init; }

    public required SelectionPath Path { get; init; }
}

public abstract class SelectionNode : PlanNode
{
    public required string SchemaName { get; init; }

    public abstract ISyntaxNode SyntaxNode { get; }

    public int PathCost { get; init; }

    public required ImmutableSortedSet<BacklogItem> Backlog { get; init; }

    public int TotalCost => PathCost + Backlog.Count;
}

public class FieldPlanNode : SelectionNode
{
    public required FieldNode FieldNode { get; init; }

    public override ISyntaxNode SyntaxNode => FieldNode;

    public required CompositeOutputField Field { get; init; }
}


public sealed record BacklogItem(int Priority, ISyntaxNode Parent, ISyntaxNode Node, string? IgnoreSchemaName = null);



public class Planner(CompositeSchema schema)
{
    private PlanNode PlanSelectionSet(SortedSet<SelectionNode> openSet)
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
                    var field = ((CompositeComplexType)type).Fields[fieldNode.Name.Value];
                    var backlogBase = current.Backlog.Remove(next);

                    foreach (var source in field.Sources)
                    {
                        if(source.SchemaName.Equals(next.IgnoreSchemaName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        var cost = current.PathCost + 1;
                        var backlog = backlogBase;

                        if(source.SchemaName != current.SchemaName)
                        {
                            cost += 10;
                            // we need to enqueue lookups in this case
                        }
                        else
                        {
                            if (source.Requirements is not null)
                            {
                                foreach (var requirement in source.Requirements.SelectionSet.Selections)
                                {
                                    backlog = backlog.Add(new BacklogItem(-1, current.SyntaxNode, requirement));
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
                    }
                    break;
            }




        }

    }

    private ICompositeNamedType GetCurrentTypeContext(SelectionNode node, BacklogItem backlogItem)
    {
        throw new InvalidOperationException();
    }
}

using System.Collections.Immutable;
using System.ComponentModel.Design;
using HotChocolate.Fusion.Planning.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Nodes3;

public abstract record PlanNode
{
    public required int Id { get; init; }

    public PlanNode? Previous { get; init; }

    public abstract ISyntaxNode SyntaxNode { get; }

    public required SelectionPath Path { get; init; }

    public required string SchemaName { get; init; }

    public int PathCost { get; init; }

    public required ImmutableSortedSet<BacklogItem> Backlog { get; init; }

    public int TotalCost => PathCost + Backlog.Count;

    // public required ImmutableDictionary<int, ImmutableList<AvailableField>> Fields { get; init; }
}

public record OperationPlanNode : PlanNode
{
    public required SelectionSetNode SelectionSet { get; init; }

    public required ICompositeNamedType Type { get; init; }

    public override ISyntaxNode SyntaxNode => SelectionSet;
}

public sealed record BacklogItem(
    int Priority,
    SelectionPath Path,
    ISyntaxNode Node,
    SelectionSetNode SelectionSet,
    IReadOnlyList<ISelectionNode> Selections,
    ICompositeNamedType Type,
    string? IgnoreSchemaName = null);

public enum BacklogItemKind
{
    Selections,
    Requirements
}

public class Planner( /*CompositeSchema schema*/)
{
    private PlanNode? PlanSelectionSet(SortedSet<PlanNode> openSet)
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

            var workItem = current.Backlog[0];
            var backlog = current.Backlog.Remove(workItem);
            var type = workItem.Type;

            switch (workItem.Node.Kind)
            {
                case SyntaxKind.OperationDefinition:
                {
                    var rootType = (CompositeObjectType)type;
                    var selections = PlanRootSelections(rootType, workItem.Selections, current.SchemaName, ref backlog);
                    var operation = new OperationPlanNode
                    {
                        Id = nextId++,
                        Previous = current,
                        Path = current.Path,
                        SchemaName = current.SchemaName,
                        PathCost = current.PathCost + 1,
                        Backlog = backlog,
                        SelectionSet = new SelectionSetNode(selections),
                        Type = rootType
                    };
                    openSet.Add(operation);
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

    private IReadOnlyList<ISelectionNode> PlanRootSelections(
        CompositeObjectType rootType,
        IReadOnlyList<ISelectionNode> selections,
        string schemaName,
        ref ImmutableSortedSet<BacklogItem> backlog)
    {
        throw new NotSupportedException();
    }

    private IReadOnlyList<ISelectionNode> PlanLookupSelections(
        CompositeComplexType complexType,
        IReadOnlyList<ISelectionNode> selections,
        string schemaName,
        ref ImmutableSortedSet<BacklogItem> backlog)
    {
        throw new NotSupportedException();
    }

    private class Rewriter
    {
        private (SelectionSetNode?, SelectionSetNode?) RewriteSelectionSet(
            RewriterContext context,
            ICompositeNamedType type,
            SelectionSetNode selectionSetNode,
            SelectionSetNode? providedSelectionSetNode)
        {
            var complexType = type as CompositeComplexType;
            List<ISelectionNode>? resolvableSelections = null;
            List<ISelectionNode>? unresolvableSelections = null;

            for (var i = 0; i < selectionSetNode.Selections.Count; i++)
            {
                var selection = selectionSetNode.Selections[i];

                switch (selection)
                {
                    case FieldNode fieldNode:
                    {
                        var (resolvable, unresolvable) =
                            RewriteFieldNode(
                                context,
                                complexType!,
                                fieldNode,
                                GetProvidedField());

                        CompleteSelection(fieldNode, resolvable, unresolvable, i);
                        break;
                    }

                    case InlineFragmentNode inlineFragmentNode:
                    {
                        var (resolvable, unresolvable) =
                            RewriteFragmentNode(
                                context,
                                type,
                                inlineFragmentNode,
                                GetProvidedFragment());

                        CompleteSelection(inlineFragmentNode, resolvable, unresolvable, i);
                        break;
                    }
                }
            }

            if (resolvableSelections is null && unresolvableSelections is null)
            {
                return (selectionSetNode, null);
            }

            return
            (
                selectionSetNode.WithSelections(resolvableSelections ?? selectionSetNode.Selections),
                unresolvableSelections is null ? null : selectionSetNode.WithSelections(unresolvableSelections)
            );

            void CompleteSelection<T>(T original, T? resolvable, T? unresolvable, int i) where T : ISelectionNode
            {
                if (unresolvable is not null)
                {
                    unresolvableSelections ??= [];
                    unresolvableSelections.Add(unresolvable);
                }

                if (resolvable is null)
                {
                    return;
                }

                if (resolvableSelections is not null)
                {
                    resolvableSelections.Add(resolvable);
                }
                else if (!ReferenceEquals(resolvable, original))
                {
                    resolvableSelections ??= [];

                    for (var j = 0; j < i; j++)
                    {
                        resolvableSelections.Add(selectionSetNode.Selections[j]);
                    }

                    resolvableSelections.Add(resolvable);
                }
            }

            FieldNode? GetProvidedField() => null;

            InlineFragmentNode? GetProvidedFragment() => null;
        }

        private (FieldNode?, FieldNode?) RewriteFieldNode(
            RewriterContext context,
            CompositeComplexType complexType,
            FieldNode fieldNode,
            FieldNode? providedFieldNode)
        {
            var field = complexType.Fields[fieldNode.Name.Value];

            if (providedFieldNode is null)
            {
                // if the field is not available in the current schema we return null
                // which will remove the field from the rewritten selection set.
                if (!field.Sources.TryGetMember(context.SchemaName, out var source))
                {
                    return (null, fieldNode);
                }

                // if requirements are not allowed we return null
                // which will remove the field from the rewritten selection set.
                if (!context.AllowRequirements && source.Requirements is not null)
                {
                    return (null, fieldNode);
                }
            }

            var selectionSet = fieldNode.SelectionSet;

            if (selectionSet is not null)
            {
                var (resolvable, unresolvable) = RewriteSelectionSet(
                    context,
                    field.Type.NamedType(),
                    selectionSet,
                    providedFieldNode?.SelectionSet);

                if (!ReferenceEquals(resolvable, selectionSet))
                {
                    return
                    (
                        fieldNode.WithSelectionSet(resolvable),
                        unresolvable is null ? null : fieldNode.WithSelectionSet(unresolvable)
                    );
                }
            }

            return (fieldNode, null);
        }

        private (InlineFragmentNode?, InlineFragmentNode?) RewriteFragmentNode(
            RewriterContext context,
            ICompositeNamedType typeCondition,
            InlineFragmentNode fieldNode,
            InlineFragmentNode? providedFieldNode)
        {

        }
    }

    private class RewriterContext
    {
        public required string SchemaName { get; init; }
        public required bool AllowRequirements { get; init; }
        public required ImmutableSortedSet<BacklogItem> Backlog { get; set; }
    }
}

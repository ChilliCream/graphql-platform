using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.WellKnownProjectionFields;

namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryablePagingProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(Selection field)
        => field is { DeclaringType: IPageType, Field.Name: "edges" or "items" or "nodes" };

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        // The selection optimizer will also process the field we just added
        // we have to avoid processing this field twice.
        if (context.ContainsResponseName(CombinedEdgeField))
        {
            return selection;
        }

        if (context.TypeContext is not IPageType pageType)
        {
            throw ThrowHelper.PagingProjectionOptimizer_NotAPagingField(
                selection.DeclaringType,
                selection.Field);
        }

        var selections = CollectSelection(context);

        var combinedSelection =
            CreateCombinedSelection(
                context,
                selection,
                pageType,
                selections);

        context.AddSelection(combinedSelection);

        return selection;
    }

    private Selection CreateCombinedSelection(
        SelectionSetOptimizerContext context,
        Selection selection,
        IPageType pageType,
        IReadOnlyList<ISelectionNode> selections)
    {
        var (fieldName, nodesField) = TryGetObjectField(pageType);

        var combinedField = new FieldNode(
            null,
            new NameNode(fieldName),
            new NameNode(CombinedEdgeField),
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            new SelectionSetNode(selections));

        var nodesPipeline =
            selection.ResolverPipeline ??
            context.CompileResolverPipeline(nodesField, combinedField);

        return new Selection(
            context.NewSelectionId(),
            CombinedEdgeField,
            nodesField,
            [new FieldSelectionNode(combinedField, 0)],
            [],
            isInternal: true,
            arguments: selection.Arguments,
            resolverPipeline: nodesPipeline);
    }

    private static (string filedName, ObjectField field) TryGetObjectField(IPageType type)
    {
        if (type.Fields.FirstOrDefault(x => x.Name == "nodes") is ObjectField nodes)
        {
            return ("nodes", nodes);
        }

        if (type.Fields.FirstOrDefault(x => x.Name == "items") is ObjectField items)
        {
            return ("items", items);
        }

        throw new GraphQLException(
            ErrorHelper.ProjectionVisitor_NodeFieldWasNotFound(type));
    }

    private IReadOnlyList<ISelectionNode> CollectSelection(SelectionSetOptimizerContext context)
    {
        var selections = new List<ISelectionNode>();

        CollectSelectionOfNodes(context, selections);
        CollectSelectionOfItems(context, selections);
        CollectSelectionOfEdges(context, selections);

        return selections;
    }

    private static void CollectSelectionOfEdges(
        SelectionSetOptimizerContext context,
        List<ISelectionNode> selections)
    {
        if (context.Selections.FirstOrDefault(t => t.Field.Name == "edges") is { } edgeSelection)
        {
            foreach (var fieldNode in edgeSelection.SyntaxNodes)
            {
                foreach (var edgeSubField in fieldNode.Node.SelectionSet!.Selections)
                {
                    if (edgeSubField is FieldNode edgeSubFieldNode
                        && edgeSubFieldNode.Name.Value is "node"
                        && edgeSubFieldNode.SelectionSet?.Selections is not null)
                    {
                        foreach (var nodeField in edgeSubFieldNode.SelectionSet.Selections)
                        {
                            selections.Add(
                                s_cloneSelectionSetRewriter.Rewrite(nodeField) ??
                                throw new SyntaxNodeCannotBeNullException(nodeField));
                        }
                    }
                }
            }
        }
    }

    private static void CollectSelectionOfItems(
        SelectionSetOptimizerContext context,
        List<ISelectionNode> selections)
    {
        if (context.Selections.FirstOrDefault(x => x.Field.Name == "items") is { } itemSelection)
        {
            foreach (var fieldNode in itemSelection.SyntaxNodes)
            {
                foreach (var nodeField in fieldNode.Node.SelectionSet!.Selections)
                {
                    selections.Add(
                        s_cloneSelectionSetRewriter.Rewrite(nodeField) ??
                        throw new SyntaxNodeCannotBeNullException(nodeField));
                }
            }
        }
    }

    private static void CollectSelectionOfNodes(
        SelectionSetOptimizerContext context,
        List<ISelectionNode> selections)
    {
        if (context.Selections.FirstOrDefault(x => x.Field.Name == "nodes") is { } nodeSelection)
        {
            foreach (var fieldNode in nodeSelection.SyntaxNodes)
            {
                foreach (var nodeField in fieldNode.Node.SelectionSet!.Selections)
                {
                    selections.Add(
                        s_cloneSelectionSetRewriter.Rewrite(nodeField) ??
                        throw new SyntaxNodeCannotBeNullException(nodeField));
                }
            }
        }
    }

    private static readonly ISyntaxRewriter<object?> s_cloneSelectionSetRewriter =
        SyntaxRewriter.Create(
            n => n.Kind is SyntaxKind.SelectionSet
                ? new SelectionSetNode(((SelectionSetNode)n).Selections)
                : n);

    public static QueryablePagingProjectionOptimizer Create(ProjectionProviderContext context) => new();
}

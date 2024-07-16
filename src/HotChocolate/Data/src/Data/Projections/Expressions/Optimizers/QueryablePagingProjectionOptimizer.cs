using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.WellKnownProjectionFields;

namespace HotChocolate.Data.Projections.Handlers;

public sealed class QueryablePagingProjectionOptimizer : IProjectionOptimizer
{
    public bool CanHandle(ISelection field) =>
        field.DeclaringType is IPageType &&
        field.Field.Name is "edges" or "items" or "nodes";

    public Selection RewriteSelection(
        SelectionSetOptimizerContext context,
        Selection selection)
    {
        // The selection optimizer will also process the field we just added
        // we have to avoid processing this field twice.
        if (context.Selections.ContainsKey(CombinedEdgeField))
        {
            return selection;
        }

        if (context.Type.NamedType() is not IPageType pageType)
        {
            throw ThrowHelper
                .PagingProjectionOptimizer_NotAPagingField(
                    selection.DeclaringType,
                    selection.Field);
        }

        var selections = CollectSelection(context);

        var combinedSelection =
            CreateCombinedSelection(
                context,
                selection,
                selection.DeclaringType,
                pageType,
                selections);

        context.AddSelection(combinedSelection);

        return selection;
    }

    private Selection CreateCombinedSelection(
        SelectionSetOptimizerContext context,
        ISelection selection,
        IObjectType declaringType,
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

        return new Selection.Sealed(
            context.GetNextSelectionId(),
            declaringType,
            nodesField,
            nodesField.Type,
            combinedField,
            CombinedEdgeField,
            resolverPipeline: nodesPipeline,
            arguments: selection.Arguments,
            isInternal: true);
    }

    private static (string filedName, IObjectField field) TryGetObjectField(IPageType type)
    {
        if (type.Fields.FirstOrDefault(x => x.Name == "nodes") is { } nodes)
        {
            return ("nodes", nodes);
        }

        if (type.Fields.FirstOrDefault(x => x.Name == "items") is { } items)
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
        if (context.Selections.Values.FirstOrDefault(
                x => x.Field.Name == "edges") is { } edgeSelection)
        {
            foreach (var edgeSubField in edgeSelection.SelectionSet!.Selections)
            {
                if (edgeSubField is FieldNode edgeSubFieldNode &&
                    edgeSubFieldNode.Name.Value is "node" &&
                    edgeSubFieldNode.SelectionSet?.Selections is not null)
                {
                    foreach (var nodeField in edgeSubFieldNode.SelectionSet.Selections)
                    {
                        selections.Add(
                            _cloneSelectionSetRewriter.Rewrite(nodeField) ??
                                throw new SyntaxNodeCannotBeNullException(nodeField));
                    }
                }
            }
        }
    }

    private static void CollectSelectionOfItems(
        SelectionSetOptimizerContext context,
        List<ISelectionNode> selections)
    {
        if (context.Selections.Values
                .FirstOrDefault(x => x.Field.Name == "items") is { } itemSelection)
        {
            foreach (var nodeField in itemSelection.SelectionSet!.Selections)
            {
                selections.Add(
                    _cloneSelectionSetRewriter.Rewrite(nodeField) ??
                        throw new SyntaxNodeCannotBeNullException(nodeField));
            }
        }
    }

    private static void CollectSelectionOfNodes(
        SelectionSetOptimizerContext context,
        List<ISelectionNode> selections)
    {
        if (context.Selections.Values.FirstOrDefault(x => x.Field.Name == "nodes") is { } nodeSelection)
        {
            foreach (var nodeField in nodeSelection.SelectionSet!.Selections)
            {
                selections.Add(
                    _cloneSelectionSetRewriter.Rewrite(nodeField) ??
                        throw new SyntaxNodeCannotBeNullException(nodeField));
            }
        }
    }

    private static readonly ISyntaxRewriter<object?> _cloneSelectionSetRewriter =
        SyntaxRewriter.Create(
            n => n.Kind is SyntaxKind.SelectionSet
                ? new SelectionSetNode(((SelectionSetNode)n).Selections)
                : n);
}

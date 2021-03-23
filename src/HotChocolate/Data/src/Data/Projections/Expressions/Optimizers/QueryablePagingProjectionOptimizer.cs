using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using static HotChocolate.Data.Projections.WellKnownProjectionFields;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryablePagingProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.DeclaringType is IPageType &&
            field.Field.Name.Value is "edges" or "items" or "nodes";

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            if (!(context.Type.NamedType() is IPageType pageType &&
                pageType.ItemType.NamedType() is ObjectType itemType))
            {
                throw new InvalidOperationException();
            }

            IReadOnlyList<ISelectionNode> selections = CollectSelection(context);

            context.Fields[CombinedEdgeField] =
                CreateCombinedSelection(context, selection, itemType, pageType, selections);

            return selection;
        }

        private Selection CreateCombinedSelection(
            SelectionOptimizerContext context,
            ISelection selection,
            IObjectType itemType,
            IPageType pageType,
            IReadOnlyList<ISelectionNode> selections)
        {
            (string? fieldName, IObjectField? nodesField) = TryGetObjectField(pageType);

            var combinedField = new FieldNode(
                null,
                new NameNode(fieldName),
                new NameNode(CombinedEdgeField),
                Array.Empty<DirectiveNode>(),
                Array.Empty<ArgumentNode>(),
                new SelectionSetNode(selections));

            FieldDelegate nodesPipeline =
                context.CompileResolverPipeline(nodesField, combinedField);

            return new Selection(
                itemType,
                nodesField,
                combinedField,
                nodesPipeline,
                arguments: selection.Arguments,
                internalSelection: true);
        }

        private (string filedName, IObjectField field) TryGetObjectField(IPageType type)
        {
            if (type.Fields.ContainsField("nodes"))
            {
                return ("nodes", type.Fields["nodes"]);
            }

            if (type.Fields.ContainsField("items"))
            {
                return ("items", type.Fields["items"]);
            }

            throw new GraphQLException(
                ErrorHelper.ProjectionVisitor_NodeFieldWasNotFound(type));
        }

        private IReadOnlyList<ISelectionNode> CollectSelection(SelectionOptimizerContext context)
        {
            var selections = new List<ISelectionNode>();

            CollectSelectionOfNodes(context, selections);
            CollectSelectionOfItems(context, selections);
            CollectSelectionOfEdges(context, selections);

            return selections;
        }

        private static void CollectSelectionOfEdges(
            SelectionOptimizerContext context,
            List<ISelectionNode> selections)
        {
            if (context.Fields.TryGetValue("edges", out Selection? edgeSelection))
            {
                foreach (var edgeSubField in edgeSelection.SelectionSet.Selections)
                {
                    if (edgeSubField is FieldNode edgeSubFieldNode &&
                        edgeSubFieldNode.Name.Value is "node" &&
                        edgeSubFieldNode.SelectionSet?.Selections is not null)
                    {
                        foreach (var nodeField in edgeSubFieldNode.SelectionSet.Selections)
                        {
                            selections.Add(
                                CloneSelectionSetVisitor.Default.CloneSelectionNode(nodeField));
                        }
                    }
                }
            }
        }

        private static void CollectSelectionOfItems(
            SelectionOptimizerContext context,
            List<ISelectionNode> selections)
        {
            if (context.Fields.TryGetValue("items", out Selection? itemSelection))
            {
                foreach (var nodeField in itemSelection.SelectionSet.Selections)
                {
                    selections.Add(CloneSelectionSetVisitor.Default.CloneSelectionNode(nodeField));
                }
            }
        }

        private static void CollectSelectionOfNodes(
            SelectionOptimizerContext context,
            List<ISelectionNode> selections)
        {
            if (context.Fields.TryGetValue("nodes", out Selection? nodeSelection))
            {
                foreach (var nodeField in nodeSelection.SelectionSet.Selections)
                {
                    selections.Add(CloneSelectionSetVisitor.Default.CloneSelectionNode(nodeField));
                }
            }
        }

        private class CloneSelectionSetVisitor : QuerySyntaxRewriter<object>
        {
            private static readonly object _context = new();

            protected override SelectionSetNode RewriteSelectionSet(
                SelectionSetNode node,
                object context)
            {
                return new(base.RewriteSelectionSet(node, context).Selections);
            }

            public ISelectionNode CloneSelectionNode(ISelectionNode selection)
            {
                return RewriteSelection(selection, _context);
            }

            public static readonly CloneSelectionSetVisitor Default = new();
        }
    }
}

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
            field.Field.Member is { } &&
            field.DeclaringType is IPageType &&
            field.Field.Name.Value is "edges";

        public Selection RewriteSelection(
            SelectionOptimizerContext context,
            Selection selection)
        {
            if (!(context.Type is IPageType pageType &&
                pageType.ItemType is ObjectType itemType))
            {
                throw new InvalidOperationException();
            }

            IReadOnlyList<ISelectionNode>? selections = CollectSelection(context);

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
            IObjectField nodesField = pageType.Fields["nodes"];
            var combinedField = new FieldNode(
                null,
                new NameNode("nodes"),
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

        private IReadOnlyList<ISelectionNode> CollectSelection(SelectionOptimizerContext context)
        {
            var selections = new List<ISelectionNode>();
            if (context.Fields.TryGetValue("nodes", out Selection? nodeSelection))
            {
                foreach (var nodeField in nodeSelection.SelectionSet.Selections)
                {
                    selections.Add(CloneSelectionSetVisitor.Default.CloneSelectionNode(nodeField));
                }
            }

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

            return selections;
        }

        private class CloneSelectionSetVisitor : QuerySyntaxRewriter<object>
        {
            private static readonly object _context = new();

            protected override SelectionSetNode RewriteSelectionSet(
                SelectionSetNode node,
                object context)
            {
                return new(node.Selections);
            }

            public ISelectionNode CloneSelectionNode(ISelectionNode selection)
            {
                return RewriteSelection(selection, _context);
            }

            public static readonly CloneSelectionSetVisitor Default = new();
        }
    }
}

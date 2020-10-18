using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Projections.Handlers
{
    public class QueryablePagingProjectionOptimizer : IProjectionOptimizer
    {
        public bool CanHandle(ISelection field) =>
            field.Field.Member is {} &&
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

            context.Fields.TryGetValue("nodes", out Selection? nodeSelection);
            context.Fields.TryGetValue("edges", out Selection? edgeSelection);


            var selections = new List<ISelectionNode>();
            if (edgeSelection?.SelectionSet is not null)
            {
                foreach (var edgeSubField in edgeSelection.SelectionSet.Selections)
                {
                    if (edgeSubField is FieldNode edgeSubFieldNode &&
                        edgeSubFieldNode.Name.Value is "node" &&
                        edgeSubFieldNode.SelectionSet?.Selections is not null)
                    {
                        foreach (var nodeField in edgeSubFieldNode.SelectionSet.Selections)
                        {
                            if (nodeField is FieldNode nodeFieldNode)
                            {
                                selections.Add(nodeFieldNode);
                            }
                        }
                    }
                }
            }

            if (nodeSelection is null)
            {
                IObjectField nodesField = pageType.Fields["nodes"];
                var nodesFieldNode = new FieldNode(
                    null,
                    new NameNode("nodes"),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    new SelectionSetNode(selections));

                FieldDelegate nodesPipeline =
                    context.CompileResolverPipeline(nodesField, nodesFieldNode);

                nodeSelection = new Selection(
                    itemType,
                    nodesField,
                    nodesFieldNode,
                    nodesPipeline,
                    arguments: selection.Arguments,
                    internalSelection: true);
            }
            else
            {
                if (nodeSelection.SelectionSet?.Selections is {})
                {
                    selections.AddRange(nodeSelection.SelectionSet.Selections);
                }

                nodeSelection = new Selection(
                    itemType,
                    nodeSelection.Field,
                    nodeSelection.SyntaxNode.WithSelectionSet(new SelectionSetNode(selections)),
                    nodeSelection.ResolverPipeline,
                    arguments: selection.Arguments,
                    internalSelection: false);
            }

            context.Fields["nodes"] = nodeSelection;
            return selection;
        }
    }
}

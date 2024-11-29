using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

internal sealed partial class ResolverDefinition
{
    private class ResolverRewriter : SyntaxRewriter<FetchRewriterContext>
    {
        protected override FieldNode? RewriteField(FieldNode node, FetchRewriterContext context)
        {
            var result = base.RewriteField(node, context);

            if (result is null)
            {
                return null;
            }

            if (context.Directives?.Count > 0)
            {
                result = result.WithDirectives(context.Directives);
            }

            if (context.UnspecifiedArguments?.Count > 0)
            {
                var explicitlyDefinedArguments = result.Arguments
                    .ExceptBy(context.UnspecifiedArguments, a => a.Name.Value)
                    .ToList();

                result = result.WithArguments(explicitlyDefinedArguments);
            }

            if (context.PlaceholderFound)
            {
                context.PlaceholderFound = false;

                if (context.ResponseName is not null &&
                    !node.Name.Value.EqualsOrdinal(context.ResponseName))
                {
                    return result.WithAlias(new NameNode(context.ResponseName));
                }
            }

            return result;
        }

        protected override SelectionSetNode? RewriteSelectionSet(
            SelectionSetNode node,
            FetchRewriterContext context)
        {
            var rewritten = base.RewriteSelectionSet(node, context);

            if (rewritten is not null && context.SelectionSet is not null)
            {
                List<ISelectionNode>? rewrittenList = null;
                for (var i = 0; i < rewritten.Selections.Count; i++)
                {
                    var selectionNode = rewritten.Selections[i];

                    if (rewrittenList is null)
                    {
                        if (!selectionNode.Equals(context.Placeholder, SyntaxComparison.Syntax))
                        {
                            continue;
                        }

                        // preserve selection path, so we are later able to unwrap the result.
                        var path = context.Path.ToArray();
                        context.SelectionPath = path;
                        context.PlaceholderFound = true;
                        rewrittenList = [];

                        if (context.ResponseName is not null)
                        {
                            path[^1] = context.ResponseName;
                        }

                        for (var j = 0; j < i; j++)
                        {
                            rewrittenList.Add(rewritten.Selections[j]);
                        }
                    }

                    foreach (var selection in context.SelectionSet.Selections)
                    {
                        rewrittenList.Add(selection);
                    }
                }

                return rewrittenList is null
                    ? rewritten
                    : rewritten.WithSelections(rewrittenList);
            }

            return rewritten;
        }

        protected override ISyntaxNode? OnRewrite(ISyntaxNode node, FetchRewriterContext context)
        {
            if (node is VariableNode variableNode &&
                context.Variables.TryGetValue(variableNode.Name.Value, out var valueNode))
            {
                return valueNode;
            }

            return base.OnRewrite(node, context);
        }

        protected override FetchRewriterContext OnEnter(
            ISyntaxNode node,
            FetchRewriterContext context)
        {
            if (node is FieldNode field)
            {
                context.Path.Push(field.Name.Value);
            }

            return base.OnEnter(node, context);
        }

        protected override void OnLeave(
            ISyntaxNode? node,
            FetchRewriterContext context)
        {
            if (node is FieldNode)
            {
                context.Path.Pop();
            }

            base.OnLeave(node, context);
        }
    }
}

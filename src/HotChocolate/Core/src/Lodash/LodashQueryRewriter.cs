using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class LodashQueryRewriter
    {
        private static readonly RemoveLodashDirectiveRewriter _rewriter = new();
        private static readonly object _context = new();

        public static T RemoveLodash<T>(this T node) where T : ISyntaxNode
        {
            return (T)_rewriter.Rewrite(node, _context);
        }

        private class RemoveLodashDirectiveRewriter : QuerySyntaxRewriter<object>
        {
            protected override FieldNode RewriteField(FieldNode node, object context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteField(node, context);
            }

            protected override FragmentDefinitionNode RewriteFragmentDefinition(
                FragmentDefinitionNode node,
                object context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteFragmentDefinition(node, context);
            }

            protected override FragmentSpreadNode RewriteFragmentSpread(
                FragmentSpreadNode node,
                object context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteFragmentSpread(node, context);
            }

            protected override InlineFragmentNode RewriteInlineFragment(
                InlineFragmentNode node,
                object context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteInlineFragment(node, context);
            }

            protected override OperationDefinitionNode RewriteOperationDefinition(
                OperationDefinitionNode node,
                object context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteOperationDefinition(node, context);
            }

            private bool TryRemoveDirective(
                IReadOnlyList<DirectiveNode> directives,
                [NotNullWhen(true)] out IReadOnlyList<DirectiveNode>? rewritten)
            {
                List<DirectiveNode>? newDirectives = null;
                for (var index = 0; index < directives.Count; index++)
                {
                    DirectiveNode directive = directives[index];
                    if (directive.Name.Value == WellKnownDirectiveNames.LodashDirective)
                    {
                        if (newDirectives is null)
                        {
                            newDirectives ??= new List<DirectiveNode>();
                            for (var innerIndex = 0; innerIndex < index; innerIndex++)
                            {
                                newDirectives.Add(directives[innerIndex]);
                            }
                        }
                    }
                    else
                    {
                        if (newDirectives is not null)
                        {
                            newDirectives.Add(directive);
                        }
                    }
                }

                if (newDirectives?.Count == 0)
                {
                    rewritten = Array.Empty<DirectiveNode>();
                }
                else
                {
                    rewritten = newDirectives?.ToArray();
                }

                return rewritten is not null;
            }
        }
    }
}

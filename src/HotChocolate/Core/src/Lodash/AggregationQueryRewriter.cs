using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Lodash
{
    public static class AggregationQueryRewriter
    {
        private static readonly RemoveLodashDirectiveRewriter _rewriter = new();
        private static readonly object _context = new();

        public static T RemoveLodash<T>(this T node, ISchema schema) where T : ISyntaxNode
        {
            return (T)_rewriter.Rewrite(node, AggregationQueryRewriterContext.FromSchema(schema));
        }

        private class AggregationQueryRewriterContext
        {
            private AggregationQueryRewriterContext(HashSet<string> directiveNames)
            {
                DirectiveNames = directiveNames;
            }

            public HashSet<string> DirectiveNames { get; }

            public static AggregationQueryRewriterContext FromSchema(ISchema schema) =>
                new(new HashSet<string>(schema.DirectiveTypes
                    .OfType<IAggregationDirectiveType>()
                    .Select(x => x.Name.Value)));
        }

        private class RemoveLodashDirectiveRewriter
            : QuerySyntaxRewriter<AggregationQueryRewriterContext>
        {
            protected override FieldNode RewriteField(
                FieldNode node,
                AggregationQueryRewriterContext context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    context,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteField(node, context);
            }

            protected override FragmentDefinitionNode RewriteFragmentDefinition(
                FragmentDefinitionNode node,
                AggregationQueryRewriterContext context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    context,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteFragmentDefinition(node, context);
            }

            protected override FragmentSpreadNode RewriteFragmentSpread(
                FragmentSpreadNode node,
                AggregationQueryRewriterContext context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    context,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteFragmentSpread(node, context);
            }

            protected override InlineFragmentNode RewriteInlineFragment(
                InlineFragmentNode node,
                AggregationQueryRewriterContext context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    context,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteInlineFragment(node, context);
            }

            protected override OperationDefinitionNode RewriteOperationDefinition(
                OperationDefinitionNode node,
                AggregationQueryRewriterContext context)
            {
                if (TryRemoveDirective(
                    node.Directives,
                    context,
                    out IReadOnlyList<DirectiveNode>? rewritten))
                {
                    node = node.WithDirectives(rewritten);
                }

                return base.RewriteOperationDefinition(node, context);
            }

            private bool TryRemoveDirective(
                IReadOnlyList<DirectiveNode> directives,
                AggregationQueryRewriterContext context,
                [NotNullWhen(true)] out IReadOnlyList<DirectiveNode>? rewritten)
            {
                List<DirectiveNode>? newDirectives = null;
                for (var index = 0; index < directives.Count; index++)
                {
                    DirectiveNode directive = directives[index];
                    if (context.DirectiveNames.Contains(directive.Name.Value))
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

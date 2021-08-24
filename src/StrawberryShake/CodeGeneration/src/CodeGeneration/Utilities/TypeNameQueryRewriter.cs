using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class TypeNameQueryRewriter
        : QuerySyntaxRewriter<TypeNameQueryRewriter.Context>
    {
        private static readonly FieldNode _typeNameField = new(
            null,
            new NameNode(WellKnownNames.TypeName),
            null,
            Array.Empty<DirectiveNode>(),
            Array.Empty<ArgumentNode>(),
            null);

        protected override OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteOperationDefinition(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override FieldNode RewriteField(
            FieldNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteField(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override InlineFragmentNode RewriteInlineFragment(
            InlineFragmentNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteInlineFragment(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override FragmentDefinitionNode RewriteFragmentDefinition(
            FragmentDefinitionNode node,
            Context context)
        {
            context.Nodes.Push(node);
            node = base.RewriteFragmentDefinition(node, context);
            context.Nodes.Pop();
            return node;
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node,
            Context context)
        {
            SelectionSetNode current = base.RewriteSelectionSet(node, context);

            if (context.Nodes.Peek() is FieldNode &&
                !current.Selections
                    .OfType<FieldNode>()
                    .Any(t => t.Alias is null && t.Name.Value.EqualsOrdinal(WellKnownNames.TypeName)))
            {
                List<ISelectionNode> selections = current.Selections.ToList();
                selections.Insert(0, _typeNameField);
                current = current.WithSelections(selections);
            }

            return current;
        }

        public static DocumentNode Rewrite(DocumentNode document)
        {
            var rewriter = new TypeNameQueryRewriter();
            return rewriter.RewriteDocument(document, new());
        }

        public class Context
        {
            public Stack<ISyntaxNode> Nodes { get; } = new();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Utilities
{
    internal class AddTypeNameQueryRewriter
        : QuerySyntaxRewriter<object?>
    {
        private const string _typeName = "__typename";
        private OperationDefinitionNode? _operation;

        protected override OperationDefinitionNode RewriteOperationDefinition(
            OperationDefinitionNode node, object? context)
        {
            _operation = node;
            return base.RewriteOperationDefinition(node, context);
        }

        protected override SelectionSetNode RewriteSelectionSet(
            SelectionSetNode node, object? context)
        {
            SelectionSetNode current = base.RewriteSelectionSet(node, context);

            var test = node != _operation?.SelectionSet;

            if (node != _operation?.SelectionSet
                && !current.Selections.OfType<FieldNode>().Any(t =>
                    t.Alias is null && t.Name.Value.EqualsOrdinal(_typeName)))
            {
                List<ISelectionNode> selections = current.Selections.ToList();

                selections.Insert(0, new FieldNode(
                    null,
                    new NameNode(_typeName),
                    null,
                    Array.Empty<DirectiveNode>(),
                    Array.Empty<ArgumentNode>(),
                    null));

                current = current.WithSelections(selections);
            }

            return current;
        }

        public static DocumentNode Rewrite(DocumentNode document)
        {
            var rewriter = new AddTypeNameQueryRewriter();
            return rewriter.RewriteDocument(document, null);
        }
    }
}

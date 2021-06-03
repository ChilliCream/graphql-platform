using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class RemoveClientDirectivesRewriter
        : QuerySyntaxRewriter<object?>
    {
        private const string _returns = "returns";

        protected override FieldNode RewriteField(FieldNode node, object? context)
        {
            FieldNode current = node;

            if (current.Directives.Any(t => StringExtensions.EqualsOrdinal(t.Name.Value, _returns)))
            {
                var directiveNodes = current.Directives.ToList();
                directiveNodes.RemoveAll(t => t.Name.Value.EqualsOrdinal(_returns));
                current = current.WithDirectives(directiveNodes);
            }

            return base.RewriteField(current, context);
        }

        public static DocumentNode Rewrite(DocumentNode document)
        {
            var rewriter = new RemoveClientDirectivesRewriter();
            return rewriter.RewriteDocument(document, null);
        }
    }
}

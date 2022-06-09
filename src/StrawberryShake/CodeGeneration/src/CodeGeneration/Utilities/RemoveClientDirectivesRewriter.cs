using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class RemoveClientDirectivesRewriter : SyntaxRewriter<ISyntaxVisitorContext>
{
    private const string _returns = "returns";

    protected override FieldNode RewriteField(FieldNode node, ISyntaxVisitorContext context)
    {
        FieldNode current = node;

        if (current.Directives.Any(t => t.Name.Value.EqualsOrdinal(_returns)))
        {
            var directiveNodes = current.Directives.ToList();
            directiveNodes.RemoveAll(static t => t.Name.Value.EqualsOrdinal(_returns));
            current = current.WithDirectives(directiveNodes);
        }

        return base.RewriteField(current, context);
    }

    public static DocumentNode Rewrite(DocumentNode document)
        => new RemoveClientDirectivesRewriter().Rewrite(document);
}

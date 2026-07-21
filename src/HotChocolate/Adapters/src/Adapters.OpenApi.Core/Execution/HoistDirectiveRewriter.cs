using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class HoistDirectiveRewriter : SyntaxRewriter<object?>
{
    public static HoistDirectiveRewriter Instance { get; } = new();

    public DocumentNode Rewrite(DocumentNode document)
        => (DocumentNode)Rewrite(document, null)!;

    protected override FieldNode? RewriteField(FieldNode node, object? context)
    {
        var rewritten = base.RewriteField(node, context)!;
        if (!rewritten.Directives.Any(d => d.Name.Value == WellKnownDirectiveNames.Hoist))
        {
            return rewritten;
        }

        return rewritten.WithDirectives(
            rewritten.Directives
                .Where(d => d.Name.Value != WellKnownDirectiveNames.Hoist)
                .ToArray());
    }
}

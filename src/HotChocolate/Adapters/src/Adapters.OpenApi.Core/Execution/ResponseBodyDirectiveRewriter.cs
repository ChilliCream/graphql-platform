using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Adapters.OpenApi;

internal sealed class ResponseBodyDirectiveRewriter : SyntaxRewriter<object?>
{
    public static ResponseBodyDirectiveRewriter Instance { get; } = new();

    public DocumentNode Rewrite(DocumentNode document)
        => (DocumentNode)Rewrite(document, null)!;

    protected override FieldNode? RewriteField(FieldNode node, object? context)
    {
        var rewritten = base.RewriteField(node, context)!;
        if (!rewritten.Directives.Any(
                d => d.Name.Value == WellKnownDirectiveNames.ResponseBody))
        {
            return rewritten;
        }

        return rewritten.WithDirectives(
            rewritten.Directives
                .Where(d => d.Name.Value != WellKnownDirectiveNames.ResponseBody)
                .ToArray());
    }
}

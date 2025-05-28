using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.SyntaxRewriters;

public sealed class RemoveDirectiveNodesSyntaxRewriter : SyntaxRewriter<object?>
{
    protected override DirectiveNode? RewriteDirective(DirectiveNode node, object? context)
    {
        return null;
    }
}

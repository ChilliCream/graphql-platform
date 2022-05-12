using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Directives;

public class IgnoreDirective
{
    private static readonly NameNode _ignoreNameNode = new("ignore");

    public IgnoreDirective(DirectiveNode directiveNode)
    {
        Directive = directiveNode;
    }

    public DirectiveNode Directive { get; }

    public static bool TryParse(ISyntaxNode syntaxNode, [MaybeNullWhen(false)] out IgnoreDirective renameDirective)
    {
        if (syntaxNode is not DirectiveNode directiveNode)
        {
            renameDirective = default;
            return false;
        }

        return TryParse(directiveNode, out renameDirective);
    }

    public static bool TryParse(DirectiveNode directiveNode, [MaybeNullWhen(false)] out IgnoreDirective renameDirective)
    {
        if (!SyntaxComparer.BySyntax.Equals(directiveNode.Name, _ignoreNameNode))
        {
            renameDirective = default;
            return false;
        }

        renameDirective = new IgnoreDirective(directiveNode);
        return true;
    }
}

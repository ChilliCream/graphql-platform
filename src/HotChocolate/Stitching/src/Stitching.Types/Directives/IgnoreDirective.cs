using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Directives;

public class IgnoreDirective : ISyntaxNode
{
    private static readonly NameNode _ignoreNameNode = new("ignore");

    public IgnoreDirective(DirectiveNode directiveNode)
    {
        Directive = directiveNode;
    }

    public DirectiveNode Directive { get; }

    public static bool TryParse(ISyntaxNode syntaxNode,
        [MaybeNullWhen(false)] out IgnoreDirective renameDirective)
    {
        if (syntaxNode is not DirectiveNode directiveNode)
        {
            renameDirective = default;
            return false;
        }

        return TryParse(directiveNode, out renameDirective);
    }

    public static bool TryParse(DirectiveNode directiveNode,
        [MaybeNullWhen(false)] out IgnoreDirective renameDirective)
    {
        if (!SyntaxComparer.BySyntax.Equals(directiveNode.Name, _ignoreNameNode))
        {
            renameDirective = default;
            return false;
        }

        renameDirective = new IgnoreDirective(directiveNode);
        return true;
    }

    public SyntaxKind Kind => SyntaxKind.Directive;
    public Language.Location? Location => default;
    public IEnumerable<ISyntaxNode> GetNodes() => Enumerable.Empty<ISyntaxNode>();

    public string ToString(bool indented) => Directive.ToString(indented);
    public override string ToString() => ToString(true);
}

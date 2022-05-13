using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Directives;

public class RenameDirective : ISyntaxNode
{
    private static readonly NameNode _renameNameNode = new("rename");
    private static readonly NameNode _nameNode = new("name");

    public RenameDirective(DirectiveNode directiveNode, StringValueNode newName)
    {
        Directive = directiveNode;
        NewName = new NameNode(newName.Value);
    }

    public DirectiveNode Directive { get; }
    public NameNode NewName { get; }

    public static bool TryParse(ISyntaxNode syntaxNode, [MaybeNullWhen(false)] out RenameDirective renameDirective)
    {
        if (syntaxNode is not DirectiveNode directiveNode)
        {
            renameDirective = default;
            return false;
        }

        return TryParse(directiveNode, out renameDirective);
    }

    public static bool TryParse(DirectiveNode directiveNode, [MaybeNullWhen(false)] out RenameDirective renameDirective)
    {
        if (!SyntaxComparer.BySyntax.Equals(directiveNode.Name, _renameNameNode))
        {
            renameDirective = default;
            return false;
        }

        IValueNode? nameArgument = directiveNode.Arguments
            .FirstOrDefault(x => x.Name.Equals(_nameNode))?.Value;

        if (nameArgument is not StringValueNode { Value.Length: > 0 } validNameArgument)
        {
            renameDirective = default;
            return false;
        }

        renameDirective = new RenameDirective(directiveNode, validNameArgument);
        return true;
    }

    public SyntaxKind Kind => SyntaxKind.Directive;
    public Language.Location? Location => default;

    public IEnumerable<ISyntaxNode> GetNodes()
    {
        return Enumerable.Empty<ISyntaxNode>();
    }

    public string ToString(bool indented) => Directive.ToString(indented);
    public override string ToString() => ToString(true);
}

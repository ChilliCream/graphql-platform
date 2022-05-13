using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Stitching.Types.Directives;

namespace HotChocolate.Stitching.Types.Visitors;

public class CollectIgnoreDirective<TContext>
    : CollectDirectiveVisitor<TContext, IgnoreDirective>
    where TContext : ISyntaxVisitorContext
{
    protected override bool ShouldCollect(ISyntaxNode directiveNode)
    {
        INamedSyntaxNode? complexType = GetAncestor<INamedSyntaxNode>();
        return complexType is not null;
    }

    protected override bool TryParseDirective(DirectiveNode directiveNode,
        [MaybeNullWhen(false)] out IgnoreDirective directive)
    {
        return IgnoreDirective.TryParse(directiveNode, out directive);
    }
}

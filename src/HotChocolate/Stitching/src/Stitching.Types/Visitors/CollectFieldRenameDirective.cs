using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Stitching.Types.Directives;

namespace HotChocolate.Stitching.Types.Visitors;

public class CollectFieldRenameDirective<TContext>
    : CollectDirectiveVisitor<TContext, RenameDirective>
    where TContext : ISyntaxVisitorContext
{
    protected override bool ShouldCollect(ISyntaxNode directiveNode)
    {
        ISyntaxNode? complexType = GetParent();
        return complexType is FieldDefinitionNode;
    }

    protected override bool TryParseDirective(DirectiveNode directiveNode,
        [MaybeNullWhen(false)] out RenameDirective directive)
    {
        return RenameDirective.TryParse(directiveNode, out directive);
    }
}

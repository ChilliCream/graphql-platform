using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.SyntaxRewriters;

/// <summary>
/// Rewrites a field definition to remove any directives and descriptions.
/// </summary>
public sealed class FusionFieldDefinitionSyntaxRewriter : SyntaxRewriter<object?>
{
    protected override DirectiveNode? RewriteDirective(DirectiveNode node, object? context)
    {
        return null;
    }

    protected override FieldDefinitionNode? RewriteFieldDefinition(FieldDefinitionNode node, object? context)
    {
        var rewrittenNode = base.RewriteFieldDefinition(node, context);
        return rewrittenNode?.WithDescription(null);
    }

    protected override InputValueDefinitionNode? RewriteInputValueDefinition(
        InputValueDefinitionNode node,
        object? context)
    {
        var rewrittenNode = base.RewriteInputValueDefinition(node, context);

        return rewrittenNode?.WithDescription(null);
    }
}

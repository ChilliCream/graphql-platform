using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ServiceConfigurationToSchemaRewriter
    : SyntaxRewriter<ConfigurationDirectiveNamesContext>
{
    protected override DirectiveNode? RewriteDirective(
        DirectiveNode node,
        ConfigurationDirectiveNamesContext context)
    {
        if (context.IsConfigurationDirective(node.Name.Value))
        {
            return null;
        }

        return base.RewriteDirective(node, context);
    }
}

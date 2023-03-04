using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FusionGraphConfigurationToSchemaRewriter
    : SyntaxRewriter<FusionGraphConfigurationToSchemaRewriter.Context>
{
    protected override DirectiveNode? RewriteDirective(
        DirectiveNode node,
        Context context)
    {
        if (context.TypeNames.IsFusionDirective(node.Name.Value))
        {
            return null;
        }

        return base.RewriteDirective(node, context);
    }

    internal sealed class Context : ISyntaxVisitorContext
    {
        public Context(FusionTypeNames typeNames)
        {
            TypeNames = typeNames;
        }

        public FusionTypeNames TypeNames { get; }
    }
}

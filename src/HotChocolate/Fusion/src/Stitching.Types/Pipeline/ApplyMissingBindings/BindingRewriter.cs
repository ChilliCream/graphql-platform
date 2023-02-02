using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Stitching.Types.Pipeline.ApplyMissingBindings;

internal sealed class BindingRewriter : SyntaxRewriter<BindingContext>
{
    protected override FieldDefinitionNode RewriteFieldDefinition(
        FieldDefinitionNode node,
        BindingContext context)
    {
        node = base.RewriteFieldDefinition(node, context);

        if (node.Directives.Count == 0)
        {
            node = node.WithDirectives(
                new DirectiveNode[]
                {
                    new BindDirective(context.SchemaName)
                });
        }
        else
        {
            for (var i = 0; i < node.Directives.Count; i++)
            {
                if (BindDirective.IsOfType(node.Directives[i]))
                {
                    goto EXIT;
                }
            }

            var temp = node.Directives.ToList();
            temp.Add(new BindDirective(context.SchemaName));
            node = node.WithDirectives(temp);
        }

        EXIT:
        return node;
    }
}

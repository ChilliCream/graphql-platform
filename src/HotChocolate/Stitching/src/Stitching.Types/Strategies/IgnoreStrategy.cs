using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Stitching.Types.Directives;
using HotChocolate.Stitching.Types.Rewriters;

namespace HotChocolate.Stitching.Types.Strategies;

public class IgnoreStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IList<SyntaxReference> renames = Get(source);
        var visitor = new IgnoreNode<Context>(renames);
        TNode node = visitor.Rewrite(source, new DefaultSyntaxNavigator(), new Context());
        var cleanup = new RemoveInterfaceReferences<Context>(visitor.IgnoredInterfaces);
        return cleanup.Rewrite(node, new DefaultSyntaxNavigator(), new Context());
    }

    private IList<SyntaxReference> Get(ISyntaxNode source)
    {
        return GetDescendants(source)
            .Where(reference => reference.Parent?.Node is ComplexTypeDefinitionNodeBase or FieldDefinitionNode
                                && reference.Node is DirectiveNode directiveNode
                                && IgnoreDirective.TryParse(directiveNode, out _))
            .ToList();
    }
}

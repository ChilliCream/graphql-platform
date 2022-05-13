using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Stitching.Types.Rewriters;
using HotChocolate.Stitching.Types.Visitors;

namespace HotChocolate.Stitching.Types.Strategies;

public class IgnoreStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IReadOnlyList<SyntaxReference> ignores = Get(source);
        var visitor = new IgnoreNode<Context>(ignores);
        TNode node = visitor.Rewrite(source, new DefaultSyntaxNavigator(), new Context());
        var cleanup = new RemoveInterfaceReferences<Context>(visitor.IgnoredInterfaces);
        return cleanup.Rewrite(node, new DefaultSyntaxNavigator(), new Context());
    }

    private IReadOnlyList<SyntaxReference> Get(ISyntaxNode source)
    {
        var collectDirectiveVisitor = new CollectIgnoreDirective<Context>();
        collectDirectiveVisitor.Visit(source, new Context());
        return collectDirectiveVisitor.Directives;
    }
}

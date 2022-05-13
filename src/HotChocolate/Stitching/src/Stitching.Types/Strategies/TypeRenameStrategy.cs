using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Stitching.Types.Rewriters;
using HotChocolate.Stitching.Types.Visitors;

namespace HotChocolate.Stitching.Types.Strategies;

public class TypeRenameStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IReadOnlyList<SyntaxReference> renames = Get(source);
        var visitor = new RenameTypes<Context>(renames);
        return visitor.Rewrite(source, new DefaultSyntaxNavigator(), new Context());
    }

    private static IReadOnlyList<SyntaxReference> Get(ISyntaxNode source)
    {
        var collectDirectiveVisitor = new CollectTypeRenameDirective<Context>();
        collectDirectiveVisitor.Visit(source, new Context());
        return collectDirectiveVisitor.Directives;
    }
}

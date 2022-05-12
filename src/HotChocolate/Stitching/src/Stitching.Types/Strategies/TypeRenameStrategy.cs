using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Stitching.Types.Directives;
using HotChocolate.Stitching.Types.Rewriters;

namespace HotChocolate.Stitching.Types.Strategies;

public class TypeRenameStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IList<SyntaxReference> renames = Get(source);
        var visitor = new RenameTypes<Context>(renames);
        return visitor.Rewrite(source, new DefaultSyntaxNavigator(), new Context());
    }

    private IList<SyntaxReference> Get(ISyntaxNode source)
    {
        return GetDescendants(source)
            .Where(reference => reference.Parent?.Node is ComplexTypeDefinitionNodeBase
                                && reference.Node is DirectiveNode directiveNode
                                && RenameDirective.TryParse(directiveNode, out _))
            .ToList();
    }
}

using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Rewriters;
using HotChocolate.Language.Rewriters.Contracts;

namespace HotChocolate.Stitching.Types;

public class TypeRenameStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IList<ISyntaxReference> renames = Get(source);
        var visitor = new RenameTypes<Context>(renames);
        return visitor.Rewrite(source, new DefaultSyntaxNavigator(), new Context());
    }

    private IList<ISyntaxReference> Get(ISyntaxNode source)
    {
        return GetDescendants(source)
            .Where(reference => reference.Parent?.Node is ComplexTypeDefinitionNodeBase
                                && reference.Node is DirectiveNode directiveNode
                                && directiveNode.Name.Equals(new NameNode("rename")))
            .ToList();
    }
}

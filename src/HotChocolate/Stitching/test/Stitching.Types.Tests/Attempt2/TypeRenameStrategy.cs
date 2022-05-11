using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class TypeRenameStrategy : SchemaRewriteStrategyBase
{
    public TNode Apply<TNode>(TNode source)
        where TNode : class, ISyntaxNode
    {
        IList<SyntaxReference> renames = Get(source);
        var visitor = new RenameTypes<Context>(renames);
        return visitor.Rewrite(source, new Context()) as TNode;
    }

    private IList<SyntaxReference> Get(ISyntaxNode source)
    {
        return GetDescendants(source)
            .Where(reference => reference.Parent.Node is ComplexTypeDefinitionNodeBase
                                && reference.Node is DirectiveNode directiveNode
                                && directiveNode.Name.Equals(new NameNode("rename")))
            .ToList();
    }
}
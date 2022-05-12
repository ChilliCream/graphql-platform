using System.Collections.Generic;
using HotChocolate.Language.Rewriters.Contracts;

namespace HotChocolate.Language.Rewriters;

public class SchemaRewriteStrategyBase
{
    protected IEnumerable<ISyntaxReference> GetDescendants(ISyntaxNode node)
    {
        return GetDescendants(new SyntaxReference(default, node), node);
    }

    protected IEnumerable<ISyntaxReference> GetDescendants(ISyntaxReference? parent, ISyntaxNode node)
    {
        var self = new SyntaxReference(parent, node);
        foreach (ISyntaxNode child in node.GetNodes())
        {
            yield return new SyntaxReference(self, child);
        }

        foreach (ISyntaxNode child in node.GetNodes())
        {
            IEnumerable<ISyntaxReference> grandChildren = GetDescendants(self, child);
            foreach (ISyntaxReference grandChild in grandChildren)
            {
                yield return grandChild;
            }
        }
    }
}

using System.Collections.Generic;

namespace HotChocolate.Language.Rewriters;

public class SchemaRewriteStrategyBase
{
    protected IEnumerable<SyntaxReference> GetDescendants(ISyntaxNode node)
    {
        return GetDescendants(new SyntaxReference(default, node), node);
    }

    protected IEnumerable<SyntaxReference> GetDescendants(SyntaxReference? parent, ISyntaxNode node)
    {
        var self = new SyntaxReference(parent, node);
        foreach (ISyntaxNode child in node.GetNodes())
        {
            yield return new SyntaxReference(self, child);
        }

        foreach (ISyntaxNode child in node.GetNodes())
        {
            IEnumerable<SyntaxReference> grandChildren = GetDescendants(self, child);
            foreach (SyntaxReference grandChild in grandChildren)
            {
                yield return grandChild;
            }
        }
    }
}

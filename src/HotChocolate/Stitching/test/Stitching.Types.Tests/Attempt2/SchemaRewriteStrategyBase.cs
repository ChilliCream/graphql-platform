using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Types.Attempt2;

public class SchemaRewriteStrategyBase
{
    protected IEnumerable<SyntaxReference> GetDescendants(ISyntaxNode node)
    {
        return GetDescendants(new SyntaxReference(default, node), node);
    }

    protected IEnumerable<SyntaxReference> GetDescendants(ISyntaxReference? parent, ISyntaxNode node)
    {
        var me = new SyntaxReference(parent, node);
        foreach (ISyntaxNode child in node.GetNodes())
        {
            yield return new SyntaxReference(me, child);
        }

        foreach (ISyntaxNode child in node.GetNodes())
        {
            IEnumerable<SyntaxReference> grandChildren = GetDescendants(me, child);
            foreach (SyntaxReference grandChild in grandChildren)
            {
                yield return grandChild;
            }
        }
    }
}
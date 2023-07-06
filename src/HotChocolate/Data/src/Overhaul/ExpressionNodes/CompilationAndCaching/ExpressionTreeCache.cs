using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public struct CachedExpression
{
    public Expression Expression { get; set; }
}

internal sealed class ExpressionTreeCache
{
    public SealedMetaTree Tree { get; }
    public CachedExpression[] CachedExpressions { get; }
    public VariableContext Variables { get; }
    public HashSet<Identifier> ValuesChanged { get; } = new();
    public ExpressionCompilationContext Context { get; }
    public bool IsFirstUse { get; set; } = true;

    public ExpressionTreeCache(
        SealedMetaTree tree,
        CachedExpression[] cachedExpressions,
        VariableContext variables)
    {
        Tree = tree;
        CachedExpressions = cachedExpressions;
        Variables = variables;
        Context = new(this, Tree);
    }
}

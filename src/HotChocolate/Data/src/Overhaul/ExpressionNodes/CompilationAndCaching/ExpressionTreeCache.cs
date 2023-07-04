using System.Collections.Generic;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

public struct CachedExpression
{
    public Expression Expression { get; set; }
}

internal sealed class ExpressionTreeCache
{
    public CachedExpression[] CachedExpressions { get; }
    public IVariableContext Variables { get; }
    public HashSet<Identifier> ValuesChanged { get; } = new();
    public bool AllValuesChanged { get; set; }
    public ExpressionCompilationContext Context { get; internal set; } = null!;

    public ExpressionTreeCache(
        CachedExpression[] cachedExpressions,
        IVariableContext variables)
    {
        CachedExpressions = cachedExpressions;
        Variables = variables;
    }
}

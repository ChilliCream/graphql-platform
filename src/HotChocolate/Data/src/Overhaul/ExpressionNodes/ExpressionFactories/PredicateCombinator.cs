using System.Collections;
using System.Diagnostics;
using System.Linq.Expressions;

namespace HotChocolate.Data.ExpressionNodes;

// A marker interface, might come useful later.
public interface IPredicateExpressionFactory : IExpressionFactory
{
}

[NoStructuralDependencies]
public sealed class PredicateCombinator : IPredicateExpressionFactory
{
    private readonly BitArray _combineWithAnd;

    public PredicateCombinator(BitArray combineWithAnd)
    {
        _combineWithAnd = combineWithAnd;
    }

    public Expression GetExpression(IExpressionCompilationContext context)
    {
        var children = context.Expressions.Children;
        Debug.Assert(children.Count == _combineWithAnd.Count + 1);
        Debug.Assert(children.Count > 0);

        var current = children[0];

        for (int i = 1; i < children.Count; i++)
        {
            bool combineWithAnd = _combineWithAnd[i];
            var child = children[i];

            if (combineWithAnd)
                current = Expression.And(current, child);
            else
                current = Expression.Or(current, child);
        }

        return current;
    }
}

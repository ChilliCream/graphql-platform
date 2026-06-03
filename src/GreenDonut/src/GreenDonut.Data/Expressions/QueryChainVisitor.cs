using System.Linq.Expressions;

namespace GreenDonut.Data.Expressions;

/// <summary>
/// A base class for expression visitors that inspect or rewrite the top-level
/// method chain of a query. Order operations that are relevant for pagination
/// only appear on this chain. Lambda arguments (predicates, projections, key
/// selectors) belong to their operator and are not traversed automatically;
/// a visitor that needs an order key lambda inspects it explicitly.
/// </summary>
public abstract class QueryChainVisitor : ExpressionVisitor
{
    /// <summary>
    /// Returns the lambda unchanged so that lambda bodies are excluded from the
    /// traversal.
    /// </summary>
    protected sealed override Expression VisitLambda<T>(Expression<T> node) => node;
}

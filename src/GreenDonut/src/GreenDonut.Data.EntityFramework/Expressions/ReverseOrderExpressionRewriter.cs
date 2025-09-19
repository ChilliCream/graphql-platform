using System.Linq.Expressions;
using System.Reflection;

namespace GreenDonut.Data.Expressions;

public class ReverseOrderExpressionRewriter : ExpressionVisitor
{
    private static readonly MethodInfo s_orderByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderBy) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_orderByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.OrderByDescending) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_thenByMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenBy) && m.GetParameters().Length == 2);

    private static readonly MethodInfo s_thenByDescendingMethod = typeof(Queryable).GetMethods()
        .First(m => m.Name == nameof(Queryable.ThenByDescending) && m.GetParameters().Length == 2);

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var visitedArguments = node.Arguments.Select(Visit).Cast<Expression>().ToArray();

        if (node.Method.Name == nameof(Queryable.OrderBy))
        {
            return Expression.Call(
                s_orderByDescendingMethod.MakeGenericMethod(node.Method.GetGenericArguments()),
                visitedArguments);
        }

        if (node.Method.Name == nameof(Queryable.OrderByDescending))
        {
            return Expression.Call(
                s_orderByMethod.MakeGenericMethod(node.Method.GetGenericArguments()),
                visitedArguments);
        }

        if (node.Method.Name == nameof(Queryable.ThenBy))
        {
            return Expression.Call(
                s_thenByDescendingMethod.MakeGenericMethod(node.Method.GetGenericArguments()),
                visitedArguments);
        }

        if (node.Method.Name == nameof(Queryable.ThenByDescending))
        {
            return Expression.Call(
                s_thenByMethod.MakeGenericMethod(node.Method.GetGenericArguments()),
                visitedArguments);
        }

        return base.VisitMethodCall(node);
    }

    public static IQueryable<T> Rewrite<T>(IQueryable<T> query)
    {
        var reversedExpression = new ReverseOrderExpressionRewriter().Visit(query.Expression);
        return query.Provider.CreateQuery<T>(reversedExpression);
    }
}

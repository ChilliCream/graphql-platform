using System.Linq.Expressions;

namespace GreenDonut.Data;

/// <summary>
/// Rewrites sort keys so that a null receiver yields the default key value.
/// </summary>
internal sealed class NullSafeSortKeyRewriter : ExpressionVisitor
{
    private static readonly ConstantExpression s_null = Expression.Constant(null, typeof(object));
    private readonly LabelTarget _returnTarget;

    private NullSafeSortKeyRewriter(Type returnType)
    {
        _returnTarget = Expression.Label(returnType);
    }

    public static Expression<Func<TEntity, TValue>> Rewrite<TEntity, TValue>(
        Expression<Func<TEntity, TValue>> keySelector)
    {
        var rewriter = new NullSafeSortKeyRewriter(typeof(TValue));
        var body = rewriter.Visit(keySelector.Body);

        if (ReferenceEquals(body, keySelector.Body))
        {
            return keySelector;
        }

        return Expression.Lambda<Func<TEntity, TValue>>(
            Expression.Block(
                Expression.Return(rewriter._returnTarget, body),
                Expression.Label(rewriter._returnTarget, Expression.Default(typeof(TValue)))),
            keySelector.Parameters);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Expression is null or ParameterExpression)
        {
            return node;
        }

        var instance = Visit(node.Expression);

        if (instance.Type.IsValueType
            && (Nullable.GetUnderlyingType(instance.Type) is null
                || node.Member.Name == nameof(Nullable<int>.HasValue)))
        {
            return node.Update(instance);
        }

        var local = Expression.Variable(instance.Type);
        return Guard(instance, local, node.Update(local));
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var instance = Visit(node.Object);
        var arguments = Visit(node.Arguments);

        if (instance is null or ParameterExpression || instance.Type.IsValueType)
        {
            return node.Update(instance, arguments);
        }

        var local = Expression.Variable(instance.Type);
        return Guard(instance, local, node.Update(local, arguments));
    }

    protected override Expression VisitLambda<T>(Expression<T> node) => node;

    private Expression Guard(Expression instance, ParameterExpression local, Expression access)
    {
        var isNull = instance.Type.IsValueType
            ? (Expression)Expression.Not(Expression.Property(local, nameof(Nullable<int>.HasValue)))
            : Expression.ReferenceEqual(local, s_null);

        return Expression.Block(
            [local],
            Expression.Assign(local, instance),
            Expression.IfThen(
                isNull,
                Expression.Return(_returnTarget, Expression.Default(_returnTarget.Type))),
            access);
    }
}

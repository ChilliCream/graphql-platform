using System.Linq.Expressions;

namespace HotChocolate.Data.Projections.Expressions;

internal static class ProjectionExpressionBuilder
{
    private static readonly ConstantExpression s_null =
        Expression.Constant(null, typeof(object));

    public static MemberInitExpression CreateMemberInit(
        Type type,
        IEnumerable<MemberBinding> expressions)
    {
        var ctor = Expression.New(type);
        return Expression.MemberInit(ctor, expressions);
    }

    public static Expression NotNull(Expression expression)
    {
        return Expression.NotEqual(expression, s_null);
    }

    public static Expression NotNullAndAlso(Expression property, Expression condition)
    {
        return Expression.Condition(
            NotNull(property),
            condition,
            Expression.Default(property.Type));
    }
}

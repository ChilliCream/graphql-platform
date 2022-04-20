using System;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions;

internal static class ProjectionExpressionBuilder
{
    private static readonly ConstantExpression _null =
        Expression.Constant(null, typeof(object));

    public static MemberInitExpression CreateMemberInit(
        Type type,
        IEnumerable<MemberBinding> expressions)
    {
        NewExpression ctor = Expression.New(type);
        return Expression.MemberInit(ctor, expressions);
    }

    public static Expression NotNull(Expression expression)
    {
        return Expression.NotEqual(expression, _null);
    }

    public static Expression NotNullAndAlso(Expression property, MemberInfo[]? propertyKeys, Expression condition)
    {
        Expression conditionTest;

        if(propertyKeys == null)
        {
            conditionTest = NotNull(property);
        }
        else
        {
            conditionTest = propertyKeys.Aggregate(
                (Expression)Expression.Constant(true),
                (condition, member) =>
                {
                    Expression memberAccess = Expression.MakeMemberAccess(property, member);
                    var memberType = memberAccess.Type;

                    if (Nullable.GetUnderlyingType(memberType) == null)
                    {
                        memberType = typeof(Nullable<>).MakeGenericType(memberAccess.Type);

                        memberAccess = Expression.Convert(memberAccess, memberType);
                    }

                    return Expression.AndAlso(condition, Expression.NotEqual(memberAccess, Expression.Constant(null, memberType)));
                });
        }

        return Expression.Condition(
            conditionTest,
            condition,
            Expression.Default(property.Type));
    }
}

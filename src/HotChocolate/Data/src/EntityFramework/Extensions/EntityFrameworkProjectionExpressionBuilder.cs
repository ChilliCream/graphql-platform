using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Data.Projections.Expressions;

namespace HotChocolate.Data.Extensions;
internal static class EntityFrameworkProjectionExpressionBuilder
{
    public static Expression NotNullAndAlso(Expression property, MemberInfo[]? propertyKeys, Expression condition)
    {
        Expression conditionTest;

        if (propertyKeys == null)
        {
            return ProjectionExpressionBuilder.NotNullAndAlso(property, condition);
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

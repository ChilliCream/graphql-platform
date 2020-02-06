using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Types
{
    public static class SelectionExpressionBuilder
    {
        public static Expression<Func<T, T>> Project<T>(IEnumerable<MemberInfo> member)
        {
            NewExpression ctor = Expression.New(typeof(T));
            ParameterExpression parameter = Expression.Parameter(typeof(T), "x");

            MemberAssignment[] inits = member.OfType<PropertyInfo>()
                .Select(
                    x => Expression.Bind(
                        x, Expression.Property(parameter, x)))
                .ToArray();

            MemberInitExpression memberInit = Expression.MemberInit(ctor, inits);

            return Expression.Lambda<Func<T, T>>(memberInit, parameter);
        }
    }
}

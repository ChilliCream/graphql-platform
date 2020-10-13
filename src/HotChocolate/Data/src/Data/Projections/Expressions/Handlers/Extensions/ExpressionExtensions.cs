using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions.Handlers
{
    internal static class ExpressionExtensions
    {
        public static Expression Append(
            this Expression expression,
            MemberInfo? memberInfo) =>
            memberInfo switch
            {
                PropertyInfo propertyInfo => Expression.Property(expression, propertyInfo),
                MethodInfo methodInfo => Expression.Call(expression, methodInfo),
                _ => throw new InvalidOperationException()
            };

        public static Type GetReturnType(
            this MemberInfo? memberInfo) =>
            memberInfo switch
            {
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                _ => throw new InvalidOperationException()
            };
    }
}

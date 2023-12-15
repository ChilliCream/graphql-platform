using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.Projections.Expressions.Handlers;

internal static class ExpressionExtensions
{
    public static Expression Append(
        this Expression expression,
        MemberInfo? memberInfo) =>
        memberInfo switch
        {
            PropertyInfo propertyInfo => Expression.Property(expression, propertyInfo),
            MethodInfo methodInfo => Expression.Call(expression, methodInfo),
            { } info => throw ThrowHelper.ProjectionVisitor_MemberInvalid(info),
            null => throw ThrowHelper.ProjectionVisitor_NoMemberFound(),
        };

    public static Type GetReturnType(
        this MemberInfo? memberInfo) =>
        memberInfo switch
        {
            PropertyInfo propertyInfo => propertyInfo.PropertyType,
            MethodInfo methodInfo => methodInfo.ReturnType,
            { } info => throw ThrowHelper.ProjectionVisitor_MemberInvalid(info),
            null => throw ThrowHelper.ProjectionVisitor_NoMemberFound(),
        };
}

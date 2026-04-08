using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.Expressions;
using static System.Linq.Expressions.Expression;

namespace HotChocolate.Resolvers;

[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060",
    Justification = "The generic methods being specialized have no trimming constraints on their type parameters.")]
[UnconditionalSuppressMessage("AOT", "IL3050",
    Justification = "This helper builds expression trees at schema initialization time and is only used in JIT-compatible environments.")]
internal static class ResolveResultHelper
{
    private static readonly MethodInfo s_awaitTaskHelper =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitTaskHelper))!;
    private static readonly MethodInfo s_awaitValueTaskHelper =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.AwaitValueTaskHelper))!;
    private static readonly MethodInfo s_wrapResultHelper =
        typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.WrapResultHelper))!;

    public static Expression EnsureResolveResult(Expression resolver, Type result)
    {
        if (result == typeof(ValueTask<object>))
        {
            return resolver;
        }

        if (typeof(Task).IsAssignableFrom(result)
            && result.IsGenericType)
        {
            return AwaitTaskMethodCall(
                resolver,
                result.GetGenericArguments()[0]);
        }

        if (result.IsGenericType
            && result.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            return AwaitValueTaskMethodCall(
                resolver,
                result.GetGenericArguments()[0]);
        }

        return WrapResult(resolver, result);
    }

    private static MethodCallExpression AwaitTaskMethodCall(
        Expression taskExpression, Type value)
    {
        var awaitHelper = s_awaitTaskHelper.MakeGenericMethod(value);
        return Call(awaitHelper, taskExpression);
    }

    private static MethodCallExpression AwaitValueTaskMethodCall(
        Expression taskExpression, Type value)
    {
        var awaitHelper = s_awaitValueTaskHelper.MakeGenericMethod(value);
        return Call(awaitHelper, taskExpression);
    }

    private static MethodCallExpression WrapResult(
        Expression taskExpression, Type value)
    {
        var wrapResultHelper = s_wrapResultHelper.MakeGenericMethod(value);
        return Call(wrapResultHelper, taskExpression);
    }
}

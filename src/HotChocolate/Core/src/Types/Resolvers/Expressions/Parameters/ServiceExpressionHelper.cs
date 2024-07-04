using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Provides helpers for service expression builders.
/// </summary>
internal static class ServiceExpressionHelper
{
    private const string _service = nameof(IResolverContext.Service);

    private static readonly MethodInfo _getServiceMethod =
        ParameterExpressionBuilderHelpers.ContextType.GetMethods().First(
            method => method.Name.Equals(_service, StringComparison.Ordinal) &&
                method.IsGenericMethod &&
                method.GetParameters().Length == 0);

#if NET8_0_OR_GREATER
    private static readonly MethodInfo _getKeyedServiceMethod =
        ParameterExpressionBuilderHelpers.ContextType.GetMethods().First(
            method => method.Name.Equals(_service, StringComparison.Ordinal) &&
                method.IsGenericMethod &&
                method.GetParameters().Length == 1);
#endif

    /// <summary>
    /// Builds the service expression.
    /// </summary>
    public static Expression Build(
        ParameterInfo parameter,
        Expression context)
        => BuildDefaultService(parameter, context);

#if NET8_0_OR_GREATER
    /// <summary>
    /// Builds the service expression.
    /// </summary>
    public static Expression Build(
        ParameterInfo parameter,
        Expression context,
        string key)
        => BuildDefaultService(parameter, context, key);
#endif

    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getServiceMethod.MakeGenericMethod(parameterType);
        return Expression.Call(context, argumentMethod);
    }

#if NET8_0_OR_GREATER
    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context, string key)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod =  _getKeyedServiceMethod.MakeGenericMethod(parameterType);
        var keyExpression = Expression.Constant(key, typeof(object));
        return Expression.Call(context, argumentMethod, keyExpression);
    }
#endif
}

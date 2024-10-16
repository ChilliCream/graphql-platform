using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Provides helpers for service expression builders.
/// </summary>
internal static class ServiceExpressionHelper
{
    private const string _serviceResolver = nameof(GetService);
#if NET8_0_OR_GREATER
    private const string _keyedServiceResolver = nameof(GetKeyedService);
#endif
    private static readonly Expression _true = Expression.Constant(true);
    private static readonly Expression _false = Expression.Constant(false);

    private static readonly MethodInfo _getServiceMethod =
        typeof(ServiceExpressionHelper).GetMethods().First(
            method => method.Name.Equals(_serviceResolver, StringComparison.Ordinal));

#if NET8_0_OR_GREATER
    private static readonly MethodInfo _getKeyedServiceMethod =
        typeof(ServiceExpressionHelper).GetMethods().First(
            method => method.Name.Equals(_keyedServiceResolver, StringComparison.Ordinal));
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
#if NET7_0_OR_GREATER
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getServiceMethod.MakeGenericMethod(parameterType);
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(parameter);
        var isRequired = nullabilityInfo.ReadState == NullabilityState.NotNull;
        return Expression.Call(argumentMethod, context, isRequired ? _true : _false);
#else
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getServiceMethod.MakeGenericMethod(parameterType);
        return Expression.Call(argumentMethod, context, _true);
#endif
    }

#if NET8_0_OR_GREATER
    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context, string key)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = _getKeyedServiceMethod.MakeGenericMethod(parameterType);
        var keyExpression = Expression.Constant(key, typeof(object));
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(parameter);
        var isRequired = nullabilityInfo.ReadState == NullabilityState.NotNull;
        return Expression.Call(argumentMethod, context, keyExpression, isRequired ? _true : _false);
    }
#endif

    public static TService? GetService<TService>(
        IResolverContext context,
        bool required)
        where TService : notnull
    {
        return required
            ? context.Services.GetRequiredService<TService>()
            : context.Services.GetService<TService>();
    }

#if NET8_0_OR_GREATER
    public static TService? GetKeyedService<TService>(
        IResolverContext context,
        object? key,
        bool required)
        where TService : notnull
    {
        return required
            ? context.Services.GetRequiredKeyedService<TService>(key)
            : context.Services.GetKeyedService<TService>(key);
    }
#endif
}

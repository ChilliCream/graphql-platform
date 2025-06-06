using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// Provides helpers for service expression builders.
/// </summary>
internal static class ServiceExpressionHelper
{
    private const string ServiceResolver = nameof(GetService);
    private const string KeyedServiceResolver = nameof(GetKeyedService);
    private static readonly Expression s_true = Expression.Constant(true);
    private static readonly Expression s_false = Expression.Constant(false);

    private static readonly MethodInfo s_getServiceMethod =
        typeof(ServiceExpressionHelper).GetMethods().First(
            method => method.Name.Equals(ServiceResolver, StringComparison.Ordinal));

    private static readonly MethodInfo s_getKeyedServiceMethod =
        typeof(ServiceExpressionHelper).GetMethods().First(
            method => method.Name.Equals(KeyedServiceResolver, StringComparison.Ordinal));

    /// <summary>
    /// Builds the service expression.
    /// </summary>
    public static Expression Build(
        ParameterInfo parameter,
        Expression context)
        => BuildDefaultService(parameter, context);

    /// <summary>
    /// Builds the service expression.
    /// </summary>
    public static Expression Build(
        ParameterInfo parameter,
        Expression context,
        string key)
        => BuildDefaultService(parameter, context, key);

    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = s_getServiceMethod.MakeGenericMethod(parameterType);
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(parameter);
        var isRequired = nullabilityInfo.ReadState == NullabilityState.NotNull;
        return Expression.Call(argumentMethod, context, isRequired ? s_true : s_false);
    }

    private static Expression BuildDefaultService(ParameterInfo parameter, Expression context, string key)
    {
        var parameterType = parameter.ParameterType;
        var argumentMethod = s_getKeyedServiceMethod.MakeGenericMethod(parameterType);
        var keyExpression = Expression.Constant(key, typeof(object));
        var nullabilityContext = new NullabilityInfoContext();
        var nullabilityInfo = nullabilityContext.Create(parameter);
        var isRequired = nullabilityInfo.ReadState == NullabilityState.NotNull;
        return Expression.Call(argumentMethod, context, keyExpression, isRequired ? s_true : s_false);
    }

    public static TService? GetService<TService>(
        IResolverContext context,
        bool required)
        where TService : notnull
    {
        return required
            ? context.Services.GetRequiredService<TService>()
            : context.Services.GetService<TService>();
    }

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
}

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ScopedStateParameterExpressionBuilder : IParameterExpressionBuilder
{
    private static readonly MethodInfo _getScopedState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetScopedState))!;
    private static readonly MethodInfo _getScopedStateWithDefault =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetScopedStateWithDefault))!;

    protected virtual PropertyInfo ContextDataProperty { get; } =
        ContextType.GetProperty(nameof(IResolverContext.ScopedContextData))!;

    protected virtual MethodInfo SetStateMethod { get; } =
        typeof(ExpressionHelper)
            .GetMethod(nameof(ExpressionHelper.SetScopedState))!;

    protected virtual MethodInfo SetStateGenericMethod { get; } =
        typeof(ExpressionHelper)
            .GetMethod(nameof(ExpressionHelper.SetScopedStateGeneric))!;

    public virtual ArgumentKind Kind => ArgumentKind.ScopedState;

    public bool IsPure => false;

    public virtual bool IsDefaultHandler => false;

    public virtual bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ScopedStateAttribute));

    public virtual Expression Build(ParameterInfo parameter, Expression context)
    {
        var key = GetKey(parameter);

        var keyExpression =
            key is null
                ? Expression.Constant(parameter.Name, typeof(string))
                : Expression.Constant(key, typeof(string));

        return IsStateSetter(parameter.ParameterType)
            ? BuildSetter(parameter, keyExpression, context)
            : BuildGetter(parameter, keyExpression, context);
    }

    protected virtual string? GetKey(ParameterInfo parameter)
        => parameter.GetCustomAttribute<ScopedStateAttribute>()!.Key;

    protected Expression BuildSetter(
        ParameterInfo parameter,
        ConstantExpression key,
        Expression context)
    {
        var setGlobalState =
            parameter.ParameterType.IsGenericType
                ? SetStateGenericMethod.MakeGenericMethod(
                    parameter.ParameterType.GetGenericArguments()[0])
                : SetStateMethod;

        return Expression.Call(
            setGlobalState,
            context,
            key);
    }

    protected Expression BuildGetter(
        ParameterInfo parameter,
        ConstantExpression key,
        Expression context,
        Type? targetType = null)
    {
        targetType ??= parameter.ParameterType;

        var contextData = Expression.Property(context, ContextDataProperty);

        var getScopedState =
            parameter.HasDefaultValue
                ? _getScopedStateWithDefault.MakeGenericMethod(targetType)
                : _getScopedState.MakeGenericMethod(targetType);

        return parameter.HasDefaultValue
            ? Expression.Call(
                getScopedState,
                context,
                contextData,
                key,
                Expression.Constant(true, typeof(bool)),
                Expression.Constant(parameter.RawDefaultValue, targetType))
            : Expression.Call(
                getScopedState,
                context,
                contextData,
                key,
                Expression.Constant(
                    new NullableHelper(targetType).GetFlags(parameter).FirstOrDefault() ?? false,
                    typeof(bool)));
    }
}

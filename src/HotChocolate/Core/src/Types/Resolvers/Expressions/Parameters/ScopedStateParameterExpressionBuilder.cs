using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ScopedStateParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly MethodInfo _getScopedState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetScopedState))!;
    private static readonly MethodInfo _getScopedStateWithDefault =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetScopedStateWithDefault))!;
    private static readonly MethodInfo _setScopedState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetScopedState))!;
    private static readonly MethodInfo _setScopedStateGeneric =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.SetScopedStateGeneric))!;

    private static readonly PropertyInfo _contextDataProperty =
        ContextType.GetProperty(nameof(IResolverContext.ScopedContextData))!;

    protected virtual PropertyInfo ContextDataProperty => _contextDataProperty;

    protected virtual MethodInfo SetStateMethod => _setScopedState;

    protected virtual MethodInfo SetStateGenericMethod => _setScopedStateGeneric;

    public virtual ArgumentKind Kind => ArgumentKind.ScopedState;

    public bool IsPure => false;

    public virtual bool IsDefaultHandler => false;

    public virtual bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ScopedStateAttribute));

    public virtual Expression Build(ParameterExpressionBuilderContext context)
    {
        var parameter = context.Parameter;
        var key = GetKey(parameter);

        var keyExpression =
            key is null
                ? Expression.Constant(parameter.Name, typeof(string))
                : Expression.Constant(key, typeof(string));

        return IsStateSetter(parameter.ParameterType)
            ? BuildSetter(parameter, keyExpression, context.ResolverContext)
            : BuildGetter(parameter, keyExpression, context.ResolverContext);
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
                    ResolveDefaultIfNotExistsParameterValue(targetType, parameter),
                    typeof(bool)));
    }

    protected virtual bool ResolveDefaultIfNotExistsParameterValue(
        Type targetType,
        ParameterInfo parameter)
    {
        var helper = new NullableHelper(targetType);
        var nullabilityFlags = helper.GetFlags(parameter);
        if (nullabilityFlags.Length > 0 &&
            nullabilityFlags[0] is { } f)
        {
            return f;
        }
        return false;
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => throw new NotSupportedException();
}

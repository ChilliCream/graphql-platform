using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Resolvers.Expressions;
using HotChocolate.Utilities;
using Microsoft.AspNetCore.Http;


namespace HotChocolate.AspNetCore;

internal sealed class HttpContextParameterExpressionBuilder : IParameterExpressionBuilder
{
 private static readonly PropertyInfo _contextData =
        typeof(IHasContextData).GetProperty(
            nameof(IHasContextData.ContextData))!;
    private static readonly MethodInfo _getGlobalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetGlobalState))!;

    public ArgumentKind Kind => ArgumentKind.GlobalState;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpContext);

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        var key = Expression.Constant(nameof(HttpContext), typeof(string));
        var contextData = Expression.Property(context, _contextData);
        return BuildGetter(parameter, key, contextData);
    }

    private static Expression BuildGetter(
        ParameterInfo parameter,
        ConstantExpression key,
        MemberExpression contextData)
    {
        var getGlobalState = _getGlobalState.MakeGenericMethod(parameter.ParameterType);

        return Expression.Call(
            getGlobalState,
            contextData,
            key,
            Expression.Constant(
                new NullableHelper(parameter.ParameterType)
                    .GetFlags(parameter).FirstOrDefault() ?? false,
                typeof(bool)));
    }
}

internal sealed class HttpRequestParameterExpressionBuilder : IParameterExpressionBuilder
{
 private static readonly PropertyInfo _contextData =
        typeof(IHasContextData).GetProperty(
            nameof(IHasContextData.ContextData))!;
    private static readonly MethodInfo _getGlobalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetGlobalState))!;
    private static readonly MethodInfo _getGlobalStateWithDefault =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetGlobalStateWithDefault))!;

    public ArgumentKind Kind => ArgumentKind.GlobalState;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(HttpRequest);

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        var key = Expression.Constant(nameof(HttpContext), typeof(string));
        var contextData = Expression.Property(context, _contextData);
        return BuildGetter(parameter, key, contextData);
    }

    private static Expression BuildGetter(
        ParameterInfo parameter,
        ConstantExpression key,
        MemberExpression contextData)
    {
        var getGlobalState = _getGlobalState.MakeGenericMethod(parameter.ParameterType);

        return Expression.Call(
            getGlobalState,
            contextData,
            key,
            Expression.Constant(
                new NullableHelper(parameter.ParameterType)
                    .GetFlags(parameter).FirstOrDefault() ?? false,
                typeof(bool)));
    }
}

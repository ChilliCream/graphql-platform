using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class GlobalStateParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
{
    private static readonly PropertyInfo s_contextData =
        typeof(IHasContextData).GetProperty(
            nameof(IHasContextData.ContextData))!;
    private static readonly MethodInfo s_getGlobalState =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetGlobalState))!;
    private static readonly MethodInfo s_getGlobalStateWithDefault =
        typeof(ExpressionHelper).GetMethod(
            nameof(ExpressionHelper.GetGlobalStateWithDefault))!;
    private static readonly MethodInfo s_setGlobalState =
        typeof(ExpressionHelper)
            .GetMethod(nameof(ExpressionHelper.SetGlobalState))!;
    private static readonly MethodInfo s_setGlobalStateGeneric =
        typeof(ExpressionHelper)
            .GetMethod(nameof(ExpressionHelper.SetGlobalStateGeneric))!;

    public ArgumentKind Kind => ArgumentKind.GlobalState;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(GlobalStateAttribute));

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Attributes.Any(t => t is GlobalStateAttribute);

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var parameter = context.Parameter;
        var attribute = parameter.GetCustomAttribute<GlobalStateAttribute>()!;

        var key =
            attribute.Key is null
                ? Expression.Constant(parameter.Name, typeof(string))
                : Expression.Constant(attribute.Key, typeof(string));

        var contextData = Expression.Property(context.ResolverContext, s_contextData);

        return IsStateSetter(parameter.ParameterType)
            ? BuildSetter(parameter, key, contextData)
            : BuildGetter(parameter, key, contextData);
    }

    private static Expression BuildSetter(
        ParameterInfo parameter,
        ConstantExpression key,
        MemberExpression contextData)
    {
        var setGlobalState =
            parameter.ParameterType.IsGenericType
                ? s_setGlobalStateGeneric.MakeGenericMethod(
                    parameter.ParameterType.GetGenericArguments()[0])
                : s_setGlobalState;

        return Expression.Call(
            setGlobalState,
            contextData,
            key);
    }

    private static Expression BuildGetter(
        ParameterInfo parameter,
        ConstantExpression key,
        MemberExpression contextData)
    {
        var getGlobalState =
            parameter.HasDefaultValue
                ? s_getGlobalStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                : s_getGlobalState.MakeGenericMethod(parameter.ParameterType);

        return parameter.HasDefaultValue
            ? Expression.Call(
                getGlobalState,
                contextData,
                key,
                Expression.Constant(true, typeof(bool)),
                Expression.Constant(parameter.RawDefaultValue, parameter.ParameterType))
            : Expression.Call(
                getGlobalState,
                contextData,
                key,
                Expression.Constant(
                    new NullableHelper(parameter.ParameterType)
                        .GetFlags(parameter).FirstOrDefault() ?? false,
                    typeof(bool)));
    }
    public IParameterBinding Create(ParameterDescriptor parameter)
        => new ParameterBinding(this, parameter);

    private sealed class ParameterBinding : IParameterBinding
    {
        private readonly GlobalStateParameterExpressionBuilder _parent;
        private readonly string _key;

        public ParameterBinding(
            GlobalStateParameterExpressionBuilder parent,
            ParameterDescriptor parameter)
        {
            _parent = parent;

            GlobalStateAttribute? globalState = null;
            foreach (var attribute in parameter.Attributes)
            {
                if (attribute is GlobalStateAttribute casted)
                {
                    globalState = casted;
                    break;
                }
            }

            _key = globalState?.Key ?? parameter.Name;
        }

        public ArgumentKind Kind => _parent.Kind;

        public bool IsPure => _parent.IsPure;

        public T Execute<T>(IResolverContext context)
            => context.GetGlobalStateOrDefault<T>(_key, default!);
    }
}

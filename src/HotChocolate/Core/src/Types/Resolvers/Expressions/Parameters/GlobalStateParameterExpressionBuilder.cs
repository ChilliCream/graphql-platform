using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GlobalStateParameterExpressionBuilder : IParameterExpressionBuilder
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
        private static readonly MethodInfo _setGlobalState =
            typeof(ExpressionHelper)
                .GetMethod(nameof(ExpressionHelper.SetGlobalState))!;
        private static readonly MethodInfo _setGlobalStateGeneric =
            typeof(ExpressionHelper)
                .GetMethod(nameof(ExpressionHelper.SetGlobalStateGeneric))!;

        public ArgumentKind Kind => ArgumentKind.GlobalState;

        public bool IsPure => true;

        public bool CanHandle(ParameterInfo parameter)
            => parameter.IsDefined(typeof(GlobalStateAttribute));

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            GlobalStateAttribute attribute = parameter.GetCustomAttribute<GlobalStateAttribute>()!;

            ConstantExpression key =
                attribute.Key is null
                    ? Expression.Constant(parameter.Name, typeof(string))
                    : Expression.Constant(attribute.Key, typeof(string));

            MemberExpression contextData = Expression.Property(context, _contextData);

            return IsStateSetter(parameter.ParameterType)
                ? BuildSetter(parameter, key, contextData)
                : BuildGetter(parameter, key, contextData);
        }

        private Expression BuildSetter(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData)
        {
            MethodInfo setGlobalState =
                parameter.ParameterType.IsGenericType
                    ? _setGlobalStateGeneric.MakeGenericMethod(
                        parameter.ParameterType.GetGenericArguments()[0])
                    : _setGlobalState;

            return Expression.Call(
                setGlobalState,
                contextData,
                key);
        }

        private Expression BuildGetter(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData)
        {
            MethodInfo getGlobalState =
                parameter.HasDefaultValue
                    ? _getGlobalStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                    : _getGlobalState.MakeGenericMethod(parameter.ParameterType);

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
    }
}

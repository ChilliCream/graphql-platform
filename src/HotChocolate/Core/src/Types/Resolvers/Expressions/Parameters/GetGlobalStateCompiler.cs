using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetGlobalStateCompiler<T>
        : GlobalStateCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _getGlobalState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetGlobalState));
        private static readonly MethodInfo _getGlobalStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetGlobalStateWithDefault));

        protected override bool CanHandle(Type parameterType)
        {
            return !IsSetter(parameterType);
        }

        protected override Expression Compile(
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
                    key);
        }
    }
}

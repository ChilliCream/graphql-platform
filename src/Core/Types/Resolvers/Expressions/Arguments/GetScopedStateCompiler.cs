using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetScopedStateCompiler<T>
        : ScopedStateCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _getScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedState));

        private static readonly MethodInfo _getScopedStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedStateWithDefault));

        protected override bool CanHandle(
            Type parameterType) =>
            !IsSetter(parameterType);

        protected override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key)
        {
            MemberExpression contextData =
                Expression.Property(context, ScopedContextData);

            MethodInfo getGlobalState =
                parameter.HasDefaultValue
                    ? _getScopedStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                    : _getScopedState.MakeGenericMethod(parameter.ParameterType);

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

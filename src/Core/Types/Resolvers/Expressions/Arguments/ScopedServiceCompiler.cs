using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class ScopedServiceCompiler<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _getScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedState));

        private static readonly MethodInfo _getScopedStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedStateWithDefault));

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsScopedService(parameter);
        }

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            ConstantExpression key = Expression.Constant(
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name,
                typeof(string));
            MemberExpression contextData = Expression.Property(context, ScopedContextData);

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

using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SetLocalStateCompiler<T>
        : ScopedStateCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _setScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetLocalState));

        private static readonly MethodInfo _setScopedStateGeneric =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetLocalStateGeneric));

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsLocalState(parameter)
                && IsSetter(parameter.ParameterType);
        }

        protected override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key)
        {
            MethodInfo setScopedState =
                parameter.ParameterType.IsGenericType
                    ? _setScopedStateGeneric.MakeGenericMethod(
                        parameter.ParameterType.GetGenericArguments()[0])
                    : _setScopedState;

            return Expression.Call(
                setScopedState,
                context,
                key);
        }

        protected override string GetKey(ParameterInfo parameter) =>
            parameter.GetCustomAttribute<LocalStateAttribute>().Key;
    }
}

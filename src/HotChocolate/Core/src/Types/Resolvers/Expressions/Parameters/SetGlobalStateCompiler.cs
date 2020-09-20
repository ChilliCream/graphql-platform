using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SetGlobalStateCompiler<T>
        : GlobalStateCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _setGlobalState =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetGlobalState));

        private static readonly MethodInfo _setGlobalStateGeneric =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetGlobalStateGeneric));

        protected override bool CanHandle(Type parameterType)
        {
            return IsSetter(parameterType);
        }

        protected override Expression Compile(
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
    }
}
